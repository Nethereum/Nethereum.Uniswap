using System;
using Nethereum.Util;
using Nethereum.XUnitEthereumClients;
using Xunit;
using Nethereum.Contracts;
using System.Threading.Tasks;
using System.Collections.Generic;
using Nethereum.Uniswap.V2.UniswapV2Pair.ContractDefinition;
using Nethereum.Uniswap.V2.UniswapV2Factory;
using Nethereum.Uniswap.V2.UniswapV2Router02;

namespace Nethereum.Uniswap.Testing
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class UniSwapV2PairTests
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public UniSwapV2PairTests(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async void ShouldBeAbleToGetThePairForDaiWeth()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var factoryAddress = "0x5C69bEe701ef814a2B6a3EDD4B1652CB9cc5aA6f";
            var factoryService = new UniswapV2FactoryService(web3, factoryAddress);
            var weth = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
            var dai = "0x6b175474e89094c44da98b954eedeac495271d0f";
            var pair = await factoryService.GetPairQueryAsync(weth, dai);
            Assert.True(pair.IsTheSameAddress("0xa478c2975ab1ea89e8196811f51a7b7ade33eb11"));
        }


        [Fact]
        public async Task ShouldBeAbleToSwapEthForDai()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var myAddress = web3.TransactionManager.Account.Address;
            var routerV2Address = "0x7a250d5630B4cF539739dF2C5dAcb4c659F2488D";
            var uniswapV2Router02Service = new UniswapV2Router02Service(web3, routerV2Address);
            var weth = "0xc02aaa39b223fe8d0a0e5c4f27ead9083c756cc2";
            var dai = "0x6b175474e89094c44da98b954eedeac495271d0f";
            var serviceDAI = new StandardTokenEIP20.StandardTokenService(web3, dai);

            var path = new List<string> {weth, dai};
            var amountEth = Web3.Web3.Convert.ToWei(100); //10 Ether
            
            var amounts = await uniswapV2Router02Service.GetAmountsOutQueryAsync(amountEth, path);
            
            var deadline = DateTimeOffset.Now.AddMinutes(15).ToUnixTimeSeconds();
            
            var swapEthForExactTokens = new V2.UniswapV2Router02.ContractDefinition.SwapExactETHForTokensFunction()
            {
                AmountOutMin = amounts[1],
                Path = path,
                Deadline = deadline,
                To = myAddress,
                AmountToSend = amountEth
            };
           
            var balanceOriginal = await serviceDAI.BalanceOfQueryAsync(myAddress);


            var swapReceipt = await uniswapV2Router02Service.SwapExactETHForTokensRequestAndWaitForReceiptAsync(swapEthForExactTokens);
            var swapLog = swapReceipt.Logs.DecodeAllEvents<SwapEventDTO>();
            var transferLog = swapReceipt.Logs.DecodeAllEvents<TransferEventDTO>();

            var balanceNew = await serviceDAI.BalanceOfQueryAsync(myAddress);
            
            Assert.Equal(swapLog[0].Event.Amount0Out, balanceNew - balanceOriginal);

        }

        [Fact]
        public async Task ShouldBeAbleToSwapEthForDaiThenUSDC()
        {
            await ShouldBeAbleToSwapEthForDai(); //lets get some DAI


            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var myAddress = web3.TransactionManager.Account.Address;
            var routerV2Address = "0x7a250d5630B4cF539739dF2C5dAcb4c659F2488D";
            var uniswapV2Router02Service = new UniswapV2Router02Service(web3, routerV2Address);
            var usdc = "0xa0b86991c6218b36c1d19d4a2e9eb0ce3606eb48";
            var dai = "0x6b175474e89094c44da98b954eedeac495271d0f";
            var serviceDAI = new StandardTokenEIP20.StandardTokenService(web3, dai);
            var serviceUSDC = new StandardTokenEIP20.StandardTokenService(web3, usdc);

            var path = new List<string> { dai, usdc };
            var amountDAI = Web3.Web3.Convert.ToWei(10000);  //DAI 18 dec

            var amounts = await uniswapV2Router02Service.GetAmountsOutQueryAsync(amountDAI, path);

            var deadline = DateTimeOffset.Now.AddMinutes(15).ToUnixTimeSeconds();

            var swapTokensForExactTokens = new V2.UniswapV2Router02.ContractDefinition.SwapExactTokensForTokensFunction()
            {
                AmountOutMin = amounts[1],
                Path = path,
                Deadline = deadline,
                To = myAddress,
                AmountIn = amountDAI
            };

            var balanceOriginal = await serviceUSDC.BalanceOfQueryAsync(myAddress);

            var approveReceipt = await serviceDAI.ApproveRequestAndWaitForReceiptAsync(routerV2Address, amountDAI);

            var swapReceipt = await uniswapV2Router02Service.SwapExactTokensForTokensRequestAndWaitForReceiptAsync(swapTokensForExactTokens);
            var swapLog = swapReceipt.Logs.DecodeAllEvents<SwapEventDTO>();
            var transferLog = swapReceipt.Logs.DecodeAllEvents<TransferEventDTO>();

            var balanceNew = await serviceUSDC.BalanceOfQueryAsync(myAddress);

            Assert.Equal(swapLog[0].Event.Amount1Out, balanceNew - balanceOriginal);

        }

    }
}