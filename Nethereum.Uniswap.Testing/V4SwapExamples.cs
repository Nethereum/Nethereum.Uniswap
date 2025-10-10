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
    public class V4SwapExamples
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public V4SwapExamples(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task ShouldQuoteAndSwapEthForUSDCOnMainnetFork()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);

            var amountIn = Web3.Web3.Convert.ToWei(0.01);

            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, eth);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

            var usdcService = web3.Eth.ERC20.GetContractService(usdc);
            var initialUsdcBalance = await usdcService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var swapExactIn = new SwapExactIn()
            {
                CurrencyIn = eth,
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut * 95 / 100,
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
                MinAmount = 0
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalUsdcBalance = await usdcService.BalanceOfQueryAsync(account);
                var usdcReceived = finalUsdcBalance - initialUsdcBalance;

                Assert.True(usdcReceived > 0);
                Assert.True(usdcReceived >= quote.AmountOut * 95 / 100);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldSwapEthForUSDCTwice()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);

            var usdcService = web3.Eth.ERC20.GetContractService(usdc);

            for (int i = 0; i < 2; i++)
            {
                var amountIn = Web3.Web3.Convert.ToWei(0.01);
                var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, eth);

                var quoteExactParams = new QuoteExactParams()
                {
                    Path = pathKeys,
                    ExactAmount = amountIn,
                    ExactCurrency = eth,
                };

                var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

                var initialUsdcBalance = await usdcService.BalanceOfQueryAsync(account);

                var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

                var swapExactIn = new SwapExactIn()
                {
                    CurrencyIn = eth,
                    AmountIn = amountIn,
                    AmountOutMinimum = quote.AmountOut * 95 / 100,
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
                    MinAmount = 0
                };
                v4ActionBuilder.AddCommand(takeAll);

                var routerBuilder = new UniversalRouterBuilder();
                routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

                var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

                try
                {
                    var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                    var finalUsdcBalance = await usdcService.BalanceOfQueryAsync(account);
                    var usdcReceived = finalUsdcBalance - initialUsdcBalance;

                    Assert.True(usdcReceived > 0);
                    Assert.True(usdcReceived >= quote.AmountOut * 95 / 100);
                }
                catch (SmartContractCustomErrorRevertException e)
                {
                    var error = universalRouter.FindCustomErrorException(e);
                    if (error != null)
                    {
                        universalRouter.HandleCustomErrorException(e);
                    }
                    throw;
                }
            }
        }

        [Fact]
        public async Task ShouldSwapEthThroughMultiplePools()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var dai = "0x6B175474E89094C44Da98b954EedeAC495271d0F";

            var poolEthUsdc = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);
            var poolUsdcDai = V4PoolKeyHelper.CreateNormalizedForQuoter(usdc, dai, 500, 10);

            var pools = new List<PoolKey> { poolEthUsdc, poolUsdcDai };
            var amountIn = Web3.Web3.Convert.ToWei(0.01);
            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(pools, eth);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

            var daiService = web3.Eth.ERC20.GetContractService(dai);
            var initialDaiBalance = await daiService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var swapExactIn = new SwapExactIn()
            {
                CurrencyIn = eth,
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut * 95 / 100,
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
                Currency = dai,
                MinAmount = 0
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalDaiBalance = await daiService.BalanceOfQueryAsync(account);
                var daiReceived = finalDaiBalance - initialDaiBalance;

                Assert.True(daiReceived > 0);
                Assert.True(daiReceived >= quote.AmountOut * 95 / 100);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldSwapUsingSwapExactInSingle()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);

            var amountIn = Web3.Web3.Convert.ToWei(0.01);

            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, eth);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

            var usdcService = web3.Eth.ERC20.GetContractService(usdc);
            var initialUsdcBalance = await usdcService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var poolKeyForAction = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = pool.Currency0,
                Currency1 = pool.Currency1,
                Fee = pool.Fee,
                TickSpacing = pool.TickSpacing,
                Hooks = pool.Hooks
            };

            var swapExactInSingle = new SwapExactInSingle()
            {
                PoolKey = poolKeyForAction,
                ZeroForOne = true,
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut * 95 / 100,
                HookData = new byte[] { }
            };
            v4ActionBuilder.AddCommand(swapExactInSingle);

            var settleAll = new SettleAll()
            {
                Currency = eth,
                Amount = amountIn
            };
            v4ActionBuilder.AddCommand(settleAll);

            var takeAll = new TakeAll()
            {
                Currency = usdc,
                MinAmount = 0
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalUsdcBalance = await usdcService.BalanceOfQueryAsync(account);
                var usdcReceived = finalUsdcBalance - initialUsdcBalance;

                Assert.True(usdcReceived > 0);
                Assert.True(usdcReceived >= quote.AmountOut * 95 / 100);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldSwapUsingSwapExactOutSingle()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);

            var exactAmountOut = 10_000_000;

            var pathKeys = V4PathEncoder.EncodeMultihopExactOutPath(new List<PoolKey> { pool }, usdc);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = exactAmountOut,
                ExactCurrency = usdc,
            };

            var quote = await v4Quoter.QuoteExactOutputQueryAsync(quoteExactParams);

            var usdcService = web3.Eth.ERC20.GetContractService(usdc);
            var initialUsdcBalance = await usdcService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var poolKeyForAction = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = pool.Currency0,
                Currency1 = pool.Currency1,
                Fee = pool.Fee,
                TickSpacing = pool.TickSpacing,
                Hooks = pool.Hooks
            };

            var swapExactOutSingle = new SwapExactOutSingle()
            {
                PoolKey = poolKeyForAction,
                ZeroForOne = true,
                AmountOut = exactAmountOut,
                AmountInMaximum = quote.AmountIn * 105 / 100,
                HookData = new byte[] { }
            };
            v4ActionBuilder.AddCommand(swapExactOutSingle);

            var settleAll = new SettleAll()
            {
                Currency = eth,
                Amount = quote.AmountIn * 105 / 100
            };
            v4ActionBuilder.AddCommand(settleAll);

            var takeAll = new TakeAll()
            {
                Currency = usdc,
                MinAmount = exactAmountOut
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(quote.AmountIn * 105 / 100);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalUsdcBalance = await usdcService.BalanceOfQueryAsync(account);
                var usdcReceived = finalUsdcBalance - initialUsdcBalance;

                Assert.True(usdcReceived == exactAmountOut);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldSwapEthForWBTC()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var wbtc = "0x2260FAC5E5542a773Aa44fBCfeDf7C193bc2C599";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, wbtc, 3000, 60);

            var amountIn = Web3.Web3.Convert.ToWei(0.1);

            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, eth);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

            var wbtcService = web3.Eth.ERC20.GetContractService(wbtc);
            var initialWbtcBalance = await wbtcService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var poolKeyForAction = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = pool.Currency0,
                Currency1 = pool.Currency1,
                Fee = pool.Fee,
                TickSpacing = pool.TickSpacing,
                Hooks = pool.Hooks
            };

            var swapExactInSingle = new SwapExactInSingle()
            {
                PoolKey = poolKeyForAction,
                ZeroForOne = true,
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut * 95 / 100,
                HookData = new byte[] { }
            };
            v4ActionBuilder.AddCommand(swapExactInSingle);

            var settleAll = new SettleAll()
            {
                Currency = eth,
                Amount = amountIn
            };
            v4ActionBuilder.AddCommand(settleAll);

            var takeAll = new TakeAll()
            {
                Currency = wbtc,
                MinAmount = 0
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalWbtcBalance = await wbtcService.BalanceOfQueryAsync(account);
                var wbtcReceived = finalWbtcBalance - initialWbtcBalance;

                Assert.True(wbtcReceived > 0);
                Assert.True(wbtcReceived >= quote.AmountOut * 95 / 100);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldSwapWith4HopPath()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var usdt = "0xdAC17F958D2ee523a2206206994597C13D831ec7";
            var wbtc = "0x2260FAC5E5542a773Aa44fBCfeDf7C193bc2C599";

            var poolEthUsdc = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);

            var poolUsdcUsdt = V4PoolKeyHelper.CreateNormalizedForQuoter(usdc, usdt, 500, 10);

            var poolUsdtWbtc = V4PoolKeyHelper.CreateNormalizedForQuoter(usdt, wbtc, 3000, 60);

            var pools = new List<PoolKey> { poolEthUsdc, poolUsdcUsdt, poolUsdtWbtc };
            var amountIn = Web3.Web3.Convert.ToWei(0.1);
            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(pools, eth);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

            var wbtcService = web3.Eth.ERC20.GetContractService(wbtc);
            var initialWbtcBalance = await wbtcService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var swapExactIn = new SwapExactIn()
            {
                CurrencyIn = eth,
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut * 95 / 100,
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
                Currency = wbtc,
                MinAmount = 0
            };
            v4ActionBuilder.AddCommand(takeAll);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalWbtcBalance = await wbtcService.BalanceOfQueryAsync(account);
                var wbtcReceived = finalWbtcBalance - initialWbtcBalance;

                Assert.True(wbtcReceived > 0);
                Assert.True(wbtcReceived >= quote.AmountOut * 95 / 100);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldSwapUsingTakePortion()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var poolManager = new PoolManagerService(web3, UniswapAddresses.MainnetPoolManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.MainnetQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = V4PoolKeyHelper.CreateNormalizedForQuoter(eth, usdc, 500, 10);

            var amountIn = Web3.Web3.Convert.ToWei(0.01);
            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, eth);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = eth,
            };

            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);

            var usdcService = web3.Eth.ERC20.GetContractService(usdc);
            var initialUsdcBalance = await usdcService.BalanceOfQueryAsync(account);

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

            var poolKeyForAction = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = pool.Currency0,
                Currency1 = pool.Currency1,
                Fee = pool.Fee,
                TickSpacing = pool.TickSpacing,
                Hooks = pool.Hooks
            };

            var swapExactInSingle = new SwapExactInSingle()
            {
                PoolKey = poolKeyForAction,
                ZeroForOne = true,
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut * 95 / 100,
                HookData = new byte[] { }
            };
            v4ActionBuilder.AddCommand(swapExactInSingle);

            var settleAll = new SettleAll()
            {
                Currency = eth,
                Amount = amountIn
            };
            v4ActionBuilder.AddCommand(settleAll);

            var takePortion = new TakePortion()
            {
                Currency = usdc,
                Recipient = account,
                Bips = 10000
            };
            v4ActionBuilder.AddCommand(takePortion);

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            try
            {
                var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

                var finalUsdcBalance = await usdcService.BalanceOfQueryAsync(account);
                var usdcReceived = finalUsdcBalance - initialUsdcBalance;

                Assert.True(usdcReceived > 0);
                Assert.True(usdcReceived >= quote.AmountOut * 95 / 100);
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouter.FindCustomErrorException(e);
                if (error != null)
                {
                    universalRouter.HandleCustomErrorException(e);
                }
                throw;
            }
        }

        [Fact]
        public async Task ShouldQueryPositionManager()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var positionManager = new Nethereum.Uniswap.V4.PositionManager.PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var poolManager = await positionManager.PoolManagerQueryAsync();
            Assert.Equal(UniswapAddresses.MainnetPoolManagerV4, poolManager, ignoreCase: true);

            var weth9 = await positionManager.Weth9QueryAsync();
            Assert.NotNull(weth9);
            Assert.NotEqual(AddressUtil.ZERO_ADDRESS, weth9);

            var permit2 = await positionManager.Permit2QueryAsync();
            Assert.Equal(UniswapAddresses.MainnetPermit2, permit2, ignoreCase: true);

            var nextTokenId = await positionManager.NextTokenIdQueryAsync();
            Assert.True(nextTokenId >= 0);
        }

    }
}



