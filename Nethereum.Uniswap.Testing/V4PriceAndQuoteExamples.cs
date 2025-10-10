using Nethereum.Uniswap.V4.Contracts.PoolManager;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using PoolKey = Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey;
using Nethereum.Uniswap.UniversalRouter;
using Nethereum.Uniswap.V4.Mappers;
using Xunit;
using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Uniswap.V4;
using Nethereum.Uniswap.UniversalRouter.V4Actions;
using Nethereum.Contracts;
using Nethereum.XUnitEthereumClients;
using Nethereum.RPC.Extensions;
using Nethereum.Hex.HexTypes;
using Nethereum.Uniswap.V4.PositionManager;

namespace Nethereum.Uniswap.Testing
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class V4PriceAndQuoteExamples
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public V4PriceAndQuoteExamples(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task ShouldCalculatePricesWithRealQuote()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var poolKey = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);


            var amountIn = Web3.Web3.Convert.ToWei(1);
            var quoteParams = new QuoteExactSingleParams()
            {
                PoolKey = poolKey,
                ZeroForOne = true,
                ExactAmount = amountIn,
                HookData = new byte[] { }
            };

            var quote = await v4Quoter.QuoteExactInputSingleQueryAsync(quoteParams);
            Assert.True(quote.AmountOut > 0);

            var wethUsdcSqrtPriceX96 = BigInteger.Parse("1987654321098765432109876543210");
            var priceAfter = V4PriceCalculator.CalculatePriceFromSqrtPriceX96(wethUsdcSqrtPriceX96);
            Assert.True(priceAfter > 0);

            var priceAfterWithDecimals = V4PriceCalculator.CalculatePriceFromSqrtPriceX96(wethUsdcSqrtPriceX96, 18, 6);
            Assert.True(priceAfterWithDecimals > 0);

            var poolPrice = V4PriceCalculator.CreatePoolPrice(
                new byte[] { },
                eth,
                usdc,
                wethUsdcSqrtPriceX96,
                0);
            Assert.Equal(priceAfter, poolPrice.PriceCurrency0InCurrency1);
        }

        [Fact]
        public async Task ShouldCalculatePriceImpactForRealSwap()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var poolKey = new Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var amountIn = Web3.Web3.Convert.ToWei(1);
            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey> { poolKey }, eth);

            var quoteParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteParams);

            var priceBefore = 3000m;
            var effectivePrice = V4PriceImpactCalculator.CalculateEffectivePrice(amountIn, quote.AmountOut, 18, 6);
            var priceImpact = V4PriceImpactCalculator.CalculatePriceImpactFromEffectivePrices(priceBefore, effectivePrice);

            Assert.True(quote.AmountOut > 0);
            Assert.True(effectivePrice > 0);
            Assert.True(priceImpact >= 0);
        }

        [Fact]
        public async Task ShouldCreateQuoteWithSlippageProtection()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var poolKey = new Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var amountIn = Web3.Web3.Convert.ToWei(0.1);
            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey> { poolKey }, eth);

            var quoteParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteParams);

            var slippageTolerance = 1.0m;
            var priceBefore = 3000m;
            var effectivePrice = V4PriceImpactCalculator.CalculateEffectivePrice(amountIn, quote.AmountOut, 18, 6);

            var quoteWithImpact = V4PriceImpactCalculator.CreateQuoteWithImpact(
                amountIn,
                quote.AmountOut,
                slippageTolerance,
                priceBefore,
                effectivePrice,
                quote.GasEstimate);

            Assert.True(quoteWithImpact.MinimumAmountOut < quote.AmountOut);
            Assert.True(quoteWithImpact.MaximumAmountIn > amountIn);
            Assert.Equal(slippageTolerance, quoteWithImpact.SlippageTolerancePercentage);
            Assert.True(quoteWithImpact.PriceImpactPercentage >= 0);
        }

        [Fact]
        public async Task E2E_SwapWithPriceImpactAndSlippageProtection()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var poolKey = new Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var amountIn = Web3.Web3.Convert.ToWei(1);

            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(
                new List<Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey> { poolKey }, eth);

            var quoteParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteParams);

            var spotPrice = 3000m;
            var effectivePrice = V4PriceImpactCalculator.CalculateEffectivePrice(amountIn, quote.AmountOut, 18, 6);

            var priceImpactResult = V4PriceImpactCalculator.CalculatePriceImpactFromEffectivePrices(spotPrice, effectivePrice);

            var slippageTolerance = 1.0m;

            var quoteWithProtection = V4PriceImpactCalculator.CreateQuoteWithImpact(
                amountIn,
                quote.AmountOut,
                slippageTolerance,
                spotPrice,
                effectivePrice,
                quote.GasEstimate);

            Assert.True(quote.AmountOut > 0);
            Assert.True(quoteWithProtection.MinimumAmountOut < quote.AmountOut);
            Assert.True(quoteWithProtection.PriceImpactPercentage >= 0);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var swapExactIn = new SwapExactIn()
            {
                CurrencyIn = eth,
                AmountIn = amountIn,
                AmountOutMinimum = quoteWithProtection.MinimumAmountOut,
                Path = pathKeys.MapToActionV4(),
            };
            v4ActionBuilder.AddCommand(swapExactIn);

            var settleAll = new SettleAll()
            {
                Currency = eth,
                Amount = amountIn
            };
            v4ActionBuilder.AddCommand(settleAll);

            var takeAll = new TakeAll()
            {
                Currency = usdc,
                MinAmount = quoteWithProtection.MinimumAmountOut
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var usdcService = web3.Eth.ERC20.GetContractService(usdc);
            var initialUsdcBalance = await usdcService.BalanceOfQueryAsync(account);

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);
            var receipt = await universalRouter.ContractHandler.SendRequestAndWaitForReceiptAsync(executeFunction);

            Assert.True(receipt.Status.Value == 1);

            var finalUsdcBalance = await usdcService.BalanceOfQueryAsync(account);
            var actualAmountOut = finalUsdcBalance - initialUsdcBalance;

            Assert.True(actualAmountOut >= quoteWithProtection.MinimumAmountOut);

            var actualSlippage = V4SlippageCalculator.CalculateSlippagePercentage(quote.AmountOut, actualAmountOut);
            Assert.True(actualSlippage <= slippageTolerance * 1.1m);
        }

        [Fact]
        public async Task E2E_DetectHighPriceImpactAndWarn()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(1000)));

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var poolKey = new Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);

            var largeAmountIn = Web3.Web3.Convert.ToWei(100);

            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(
                new List<Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey> { poolKey }, eth);

            var quoteParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = largeAmountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteParams);

            var spotPrice = 3000m;
            var effectivePrice = V4PriceImpactCalculator.CalculateEffectivePrice(largeAmountIn, quote.AmountOut, 18, 6);

            var priceImpact = V4PriceImpactCalculator.CalculatePriceImpactFromEffectivePrices(spotPrice, effectivePrice);

            var impactLevel = V4PriceImpactCalculator.GetPriceImpactLevel(priceImpact);

            Assert.True(quote.AmountOut > 0);
            Assert.True(priceImpact > 0);
            Assert.True(impactLevel >= PriceImpactLevel.Low);
        }

        [Fact]
        public async Task E2E_CompareSmallVsLargeTradeImpact()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var poolKey = new Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(
                new List<Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey> { poolKey }, eth);

            var smallAmountIn = Web3.Web3.Convert.ToWei(0.1);
            var largeAmountIn = Web3.Web3.Convert.ToWei(10);

            var smallQuoteParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = smallAmountIn,
                ExactCurrency = eth,
            };

            var largeQuoteParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = largeAmountIn,
                ExactCurrency = eth,
            };

            var smallQuote = await v4Quoter.QuoteExactInputQueryAsync(smallQuoteParams);
            var largeQuote = await v4Quoter.QuoteExactInputQueryAsync(largeQuoteParams);

            var smallEffectivePrice = V4PriceImpactCalculator.CalculateEffectivePrice(smallAmountIn, smallQuote.AmountOut, 18, 6);
            var largeEffectivePrice = V4PriceImpactCalculator.CalculateEffectivePrice(largeAmountIn, largeQuote.AmountOut, 18, 6);

            var spotPrice = smallEffectivePrice;

            var smallImpact = V4PriceImpactCalculator.CalculatePriceImpactFromEffectivePrices(spotPrice, smallEffectivePrice);
            var largeImpact = V4PriceImpactCalculator.CalculatePriceImpactFromEffectivePrices(spotPrice, largeEffectivePrice);

            var smallLevel = V4PriceImpactCalculator.GetPriceImpactLevel(smallImpact);
            var largeLevel = V4PriceImpactCalculator.GetPriceImpactLevel(largeImpact);

            Assert.True(smallQuote.AmountOut > 0);
            Assert.True(largeQuote.AmountOut > 0);
            Assert.True(smallImpact < 0.5m);
            Assert.True(largeImpact > smallImpact);
        }

    }
}



