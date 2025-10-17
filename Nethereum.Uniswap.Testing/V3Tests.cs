using Nethereum.ABI;
using Nethereum.Contracts;
using Nethereum.Uniswap.Core.Permit2;

using Nethereum.Uniswap.V3.Contracts;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Web3.Accounts;
using System.Numerics;
using Nethereum.Uniswap.Core.Permit2.ContractDefinition;
using Xunit;
using Nethereum.Uniswap.V3;
using Nethereum.Uniswap.V3.QuoterV2;
using Nethereum.Uniswap.UniversalRouter;
using Nethereum.Uniswap.Permit2;
using Nethereum.Uniswap.UniversalRouter.Commands;

namespace Nethereum.Uniswap.Testing
{

    //For info check https://github.com/Uniswap/universal-router/tree/main
    // also https://github.com/Uniswap/permit2/tree/main
    // and uniswap docs https://docs.uniswap.org/contracts/universal-router/overview
    // and https://docs.uniswap.org/protocol/guides/swaps
    // sepolia pools https://www.geckoterminal.com/sepolia-testnet/
    public class V3Tests
    {
        [Fact]
        public async Task ShouldPermit2QuoteAndSwapUsingUniversalRouter()
        {

            var url = "https://ethereum-sepolia.rpc.subquery.network/public";
            var privateKey = "0x";
            var account = new Account(privateKey);
            var web3 = new Web3.Web3(account, url);
            var factoryAddress = UniswapAddresses.SepoliaUniswapV3Factory;
            var permit2 = UniswapAddresses.SepoliaPermit2;
            var quoterAddress = UniswapAddresses.SepoliaQuoterV2;
            var universalRouter = UniswapAddresses.SepoliaUniversalRouterV3;



            var uni = "0x1f9840a85d5af5bf1d1762f925bdaddc4201f984";
            var weth = "0xfff9976782d46cc05630d1f6ebab18b2324d6b14";


            //************ QUOTING ************


            //**** Slot 0 PriceCalculator Calculator ****
            var calculator = new UniswapV3Slot0PriceCalculator(web3, factoryAddress);
            //FEES (1% == 10000, 0.3% == 3000, 0.05% == 500, 0.01 == 100)
            var priceWethuni = await calculator.GetPoolPricesAsync(uni, weth, 500);

            //**** Quoter V2 ****
            var quoterService = new QuoterV2Service(web3, quoterAddress);

            var weth9 = await quoterService.Weth9QueryAsync();

            var amountIn = Web3.Web3.Convert.ToWei(0.001); //18 decimals.. what we are sending

            var abiEncoder = new ABIEncode();

            //path is the token pair, first the token we are sending, the pool fee, then the token we are receiving
            var path = abiEncoder.GetABIEncodedPacked(new ABIValue("address", weth9), new ABIValue("uint24", 500),
                new ABIValue("address", uni));

            var quote = await quoterService.QuoteExactInputQueryAsync(path, amountIn);

            //path is the token pair, first the token we are receiving (output), the pool fee, then the token we are sending
            var pathOutput = V3PathEncoder.EncodePath(uni, 500, weth9); //using the encoder only for 2 tokens

            var amountOut = Web3.Web3.Convert.ToWei(1, 6); //uni is 6 decimals
            var quoteAmountOut = await quoterService.QuoteExactOutputQueryAsync(pathOutput, amountOut);

            //************ SWAPPING using the Universal Router ************
            var universalRouterService = new UniversalRouterService(web3, universalRouter);
            var planner = new UniversalRouterBuilder();

            var weth9Service = web3.Eth.ERC20.GetContractService(weth9);


            BigInteger amountOfEthToSend = 0;
            amountOfEthToSend = Web3.Web3.Convert.ToWei(0.001);

            //approve permit2 for the universal router
            await weth9Service.ApproveRequestAndWaitForReceiptAsync(permit2, IntType.MAX_INT256_VALUE);

            // creating permit for this request
            var permit = new PermitSingle()
            {
                Spender = universalRouter,
                SigDeadline = 2000000000,
                Details = new PermitDetails()
                {
                    Amount = amountOfEthToSend * 100000,
                    Expiration = 0,
                    Nonce = 0, //the nonce will be set when signing
                    Token = weth9
                }
            };

            var permitService = new Permit2Service(web3, permit2);
            var signedPermit = await permitService.GetSinglePermitWithSignatureAsync(permit, new Signer.EthECKey(privateKey));

            //permit manually not using the universal router, this needs to be done before the swap and different expiration
            //try
            //{
            //   //await permitService.PermitRequestAsync(account.Address, signedPermit.PermitRequest, signedPermit.GetSignatureBytes());
            //}
            //catch (SmartContractCustomErrorRevertException e)
            //{
            //    var error = permitService.FindCustomErrorException(e);
            //    if (error != null)
            //    {
            //        Debug.WriteLine(error.Message);
            //    }

            //}


            //Starting the commands for the universal router

            //// wrap some eth to weth
            var wrapEthCommand = new WrapEthCommand()
            {
                Amount = amountOfEthToSend,
                Recipient = account.Address
            };


            planner.AddCommand(wrapEthCommand);

            //permit2 command for the universal router can spent the weth
            var permit2Command = new Permit2PermitCommand()
            {
                Permit = signedPermit.PermitRequest,
                Signature = signedPermit.GetSignatureBytes()
            };

            planner.AddCommand(permit2Command);


            //swap weth for uni
            var swapEthForuniCommand = new V3SwapExactInCommand
            {
                AmountIn = amountOfEthToSend,
                AmountOutMinimum = quote.AmountOut - 10000,// some slippage
                Path = path,
                Recipient = account.Address,
                FundsFromPermit2OrUniversalRouter = true
            };

            planner.AddCommand(swapEthForuniCommand);

            try
            {
                var receipt = await universalRouterService.ExecuteRequestAndWaitForReceiptAsync(planner.GetExecuteFunction(amountOfEthToSend));
            }
            catch (SmartContractCustomErrorRevertException e)
            {
                var error = universalRouterService.FindCustomErrorException(e);
                if (error != null)
                {
                    Debug.WriteLine(error.Message);
                    universalRouterService.HandleCustomErrorException(e);
                }
                throw;
            }

            var balanceWethWei = await weth9Service.BalanceOfQueryAsync(account.Address);
            var balanceInEth = Web3.Web3.Convert.FromWei(balanceWethWei);

            var uniService = web3.Eth.ERC20.GetContractService(uni);
            var balanceuniWei = await uniService.BalanceOfQueryAsync(account.Address);
            var balanceInuni = Web3.Web3.Convert.FromWei(balanceuniWei);
        }
    }
}
