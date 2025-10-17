using Nethereum.Uniswap.V4.Contracts.PoolManager;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using PoolKey = Nethereum.Uniswap.V4.Pricing.V4Quoter.ContractDefinition.PoolKey;
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
using Nethereum.Uniswap.Accounts;

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

            var status = await new AccountApprovalService(web3).CheckApprovalAsync(usdc, account, spender, requiredAmount);

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

            var result = await new AccountBalanceValidator(web3).ValidateBalanceAsync(eth, account, requiredAmount);

            Assert.True(result.HasSufficientBalance);
            Assert.True(result.CurrentBalance >= requiredAmount);
            Assert.Equal(BigInteger.Zero, result.Deficit);
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
