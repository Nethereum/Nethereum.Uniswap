using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using Nethereum.Web3;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using PoolKey = Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey;

namespace Nethereum.Uniswap.V4
{
    public class SwapPathResult
    {
        public List<PoolKey> Path { get; set; }
        public BigInteger AmountOut { get; set; }
        public BigInteger GasEstimate { get; set; }
        public decimal PriceImpact { get; set; }
        public int[] Fees { get; set; }
    }

    public class V4BestPathFinder
    {
        private readonly IWeb3 _web3;
        private readonly string _quoterAddress;
        private readonly V4PoolCache _poolCache;

        public V4BestPathFinder(
            IWeb3 web3,
            string quoterAddress,
            V4PoolCache poolCache)
        {
            _web3 = web3;
            _quoterAddress = quoterAddress;
            _poolCache = poolCache;
        }

        public async Task<SwapPathResult> FindBestDirectPathAsync(
            string tokenIn,
            string tokenOut,
            BigInteger amountIn,
            int[] feeTiers = null,
            int[] tickSpacings = null)
        {
            if (feeTiers == null)
            {
                feeTiers = new int[] { 100, 500, 3000, 10000 };
            }

            if (tickSpacings == null)
            {
                tickSpacings = new int[] { 1, 10, 60, 200 };
            }

            var quoter = new V4QuoterService(_web3, _quoterAddress);
            SwapPathResult bestPath = null;
            BigInteger bestAmountOut = 0;

            foreach (var fee in feeTiers)
            {
                foreach (var tickSpacing in tickSpacings)
                {
                    try
                    {
                        var pool = await _poolCache.GetOrFetchPoolAsync(
                            tokenIn,
                            tokenOut,
                            fee,
                            tickSpacing);

                        if (!pool.Exists)
                            continue;

                        var poolKey = V4PoolKeyHelper.CreateNormalizedForQuoter(
                            pool.Currency0,
                            pool.Currency1,
                            pool.Fee,
                            pool.TickSpacing,
                            pool.Hooks);

                        var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(
                            new List<PoolKey> { poolKey },
                            tokenIn);

                        var quoteParams = new QuoteExactParams
                        {
                            Path = pathKeys,
                            ExactAmount = amountIn,
                            ExactCurrency = tokenIn
                        };

                        var quote = await quoter.QuoteExactInputQueryAsync(quoteParams);

                        if (quote.AmountOut > bestAmountOut)
                        {
                            bestAmountOut = quote.AmountOut;
                            bestPath = new SwapPathResult
                            {
                                Path = new List<PoolKey> { poolKey },
                                AmountOut = quote.AmountOut,
                                GasEstimate = quote.GasEstimate,
                                Fees = new int[] { fee }
                            };
                        }
                    }
                    catch
                    {
                        continue;
                    }
                }
            }

            return bestPath;
        }

        public async Task<SwapPathResult> FindBestMultihopPathAsync(
            string tokenIn,
            string tokenOut,
            BigInteger amountIn,
            string[] intermediateTokens,
            int maxHops = 3)
        {
            var quoter = new V4QuoterService(_web3, _quoterAddress);
            SwapPathResult bestPath = null;
            BigInteger bestAmountOut = 0;

            var commonFees = new int[] { 500, 3000, 10000 };
            var commonTickSpacings = new int[] { 10, 60, 200 };

            foreach (var intermediateToken in intermediateTokens)
            {
                foreach (var fee1 in commonFees)
                {
                    foreach (var tickSpacing1 in commonTickSpacings)
                    {
                        foreach (var fee2 in commonFees)
                        {
                            foreach (var tickSpacing2 in commonTickSpacings)
                            {
                                try
                                {
                                    var pool1 = await _poolCache.GetOrFetchPoolAsync(
                                        tokenIn,
                                        intermediateToken,
                                        fee1,
                                        tickSpacing1);

                                    var pool2 = await _poolCache.GetOrFetchPoolAsync(
                                        intermediateToken,
                                        tokenOut,
                                        fee2,
                                        tickSpacing2);

                                    if (!pool1.Exists || !pool2.Exists)
                                        continue;

                                    var poolKey1 = V4PoolKeyHelper.CreateNormalizedForQuoter(
                                        pool1.Currency0,
                                        pool1.Currency1,
                                        pool1.Fee,
                                        pool1.TickSpacing,
                                        pool1.Hooks);

                                    var poolKey2 = V4PoolKeyHelper.CreateNormalizedForQuoter(
                                        pool2.Currency0,
                                        pool2.Currency1,
                                        pool2.Fee,
                                        pool2.TickSpacing,
                                        pool2.Hooks);

                                    var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(
                                        new List<PoolKey> { poolKey1, poolKey2 },
                                        tokenIn);

                                    var quoteParams = new QuoteExactParams
                                    {
                                        Path = pathKeys,
                                        ExactAmount = amountIn,
                                        ExactCurrency = tokenIn
                                    };

                                    var quote = await quoter.QuoteExactInputQueryAsync(quoteParams);

                                    if (quote.AmountOut > bestAmountOut)
                                    {
                                        bestAmountOut = quote.AmountOut;
                                        bestPath = new SwapPathResult
                                        {
                                            Path = new List<PoolKey> { poolKey1, poolKey2 },
                                            AmountOut = quote.AmountOut,
                                            GasEstimate = quote.GasEstimate,
                                            Fees = new int[] { fee1, fee2 }
                                        };
                                    }
                                }
                                catch
                                {
                                    continue;
                                }
                            }
                        }
                    }
                }
            }

            return bestPath;
        }

        public async Task<SwapPathResult> FindBestPathAsync(
            string tokenIn,
            string tokenOut,
            BigInteger amountIn,
            string[] intermediateTokens = null)
        {
            var directPath = await FindBestDirectPathAsync(tokenIn, tokenOut, amountIn);

            if (intermediateTokens == null || intermediateTokens.Length == 0)
            {
                return directPath;
            }

            var multihopPath = await FindBestMultihopPathAsync(
                tokenIn,
                tokenOut,
                amountIn,
                intermediateTokens);

            if (directPath == null)
                return multihopPath;

            if (multihopPath == null)
                return directPath;

            return multihopPath.AmountOut > directPath.AmountOut ? multihopPath : directPath;
        }
    }
}
