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
using Nethereum.RPC.Eth.DTOs;

namespace Nethereum.Uniswap.Testing
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class V4HelperExamples
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public V4HelperExamples(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestTokenApprovalHelper()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var spender = UniswapAddresses.MainnetPositionManagerV4;
            var requiredAmount = Web3.Web3.Convert.ToWei(100, Nethereum.Util.UnitConversion.EthUnit.Mwei);

            var status = await V4TokenApprovalHelper.CheckApprovalAsync(web3, usdc, account, spender, requiredAmount);

            Assert.NotNull(status);
            Assert.Equal(usdc, status.Token);
            Assert.Equal(spender, status.Spender);
            Assert.Equal(account, status.Owner);
        }

        [Fact]
        public async Task TestBalanceValidator()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(10)));

            var eth = AddressUtil.ZERO_ADDRESS;
            var requiredAmount = Web3.Web3.Convert.ToWei(5);

            var result = await V4BalanceValidator.ValidateBalanceAsync(web3, eth, account, requiredAmount);

            Assert.True(result.HasSufficientBalance);
            Assert.True(result.CurrentBalance >= requiredAmount);
            Assert.Equal(BigInteger.Zero, result.Deficit);
        }

        [Fact]
        public async Task GetEthPriceInUSDC()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var eth = AddressUtil.ZERO_ADDRESS;

            var price = await V4PriceService.GetBestPriceInStablecoinAsync(
                web3,
                UniswapAddresses.MainnetQuoterV4,
                eth,
                V4PriceService.CommonStablecoins["USDC"],
                18,
                6,
                10);

            Assert.True(price.IsValid);
            Assert.True(price.Price > 0);
        }

        [Fact]
        public async Task GetTokenPricesInAllStablecoins()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var eth = AddressUtil.ZERO_ADDRESS;

            var prices = await V4PriceService.GetPricesInAllStablecoinsAsync(
                web3,
                UniswapAddresses.MainnetQuoterV4,
                eth,
                18,
                10);

            Assert.True(prices.ContainsKey("USDC"));
            Assert.True(prices.ContainsKey("USDT"));
            Assert.True(prices.ContainsKey("DAI"));

            Assert.True(prices["USDC"].IsValid, $"USDC price not valid: {prices["USDC"].ErrorMessage}");
            Assert.True(prices["USDC"].Price > 0);

            var validPrices = new List<decimal> { prices["USDC"].Price };

            if (prices["USDT"].IsValid && prices["USDT"].Price > 0)
            {
                validPrices.Add(prices["USDT"].Price);
            }

            if (prices["DAI"].IsValid && prices["DAI"].Price > 0)
            {
                validPrices.Add(prices["DAI"].Price);
            }

            Assert.True(validPrices.Count >= 2, "At least 2 stablecoin prices should be valid");

            if (validPrices.Count >= 2)
            {
                var tolerancePercent = 5m;
                var avgPrice = validPrices.Average();

                foreach (var price in validPrices)
                {
                    var diff = Math.Abs(price - avgPrice) / avgPrice * 100m;
                    Assert.True(diff < tolerancePercent,
                        $"Price {price:F2} differs from average {avgPrice:F2} by {diff:F2}%, expected < {tolerancePercent}%");
                }
            }
        }

        [Fact]
        public async Task Integration_QueryPositionsByEvents_WithBlockRange()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();

            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var latestBlock = await web3.Eth.Blocks.GetBlockNumber.SendRequestAsync();
            var fromBlock = latestBlock.Value - 100;

            var tokenIds = await positionManager.GetPositionTokenIdsByEventsAsync(
                "0x0000000000000000000000000000000000000001",
                fromBlock,
                latestBlock.Value);

            Assert.NotNull(tokenIds);
        }

    }
}
