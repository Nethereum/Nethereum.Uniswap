using Nethereum.Uniswap.V4.Contracts.PoolManager;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using PoolKey = Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey;   
using Nethereum.Uniswap.UniversalRouter;
using Nethereum.Uniswap.V4.Mappers;
using Nethereum.Web3.Accounts;
using Xunit;
using Nethereum.Uniswap.V4.StateView;
using Nethereum.Uniswap.V4.PositionManager;
using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Uniswap.V4;
using Nethereum.Uniswap.UniversalRouter.V4Actions;

namespace Nethereum.Uniswap.Core.Tests
{
    public class V4Tests
    {
        /*[
    "0x0000000000000000000000000000000000000000",
    "0x91D1e0b9f6975655A381c79fd6f1D118D1c5b958",
    "500",
    "10",
    "0x24F7c9ea6B5be5227caAeB61366b56052386eae4"
]*/
        [Fact]
        public async Task ShouldQuoteAndSwapEthForERC20()
        {
            //https://chainlist.org/chain/1301
            var url = "https://base-sepolia.drpc.org";
            var privateKey = "0x";
            var web3 = new Web3.Web3(new Account(privateKey), url);
            var poolManager = new PoolManagerService(web3, UniswapAddresses.BaseSepoliaPoolManagerV4);
            

            var usdc = "0x91D1e0b9f6975655A381c79fd6f1D118D1c5b958";

            var pool = new PoolKey()
            {
                Currency0 = AddressUtil.ZERO_ADDRESS,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = "0x24F7c9ea6B5be5227caAeB61366b56052386eae4"
            };

            
            var stateViewService = new StateViewService(web3, UniswapAddresses.BaseSepoliaStateViewV4);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.BaseSepoliaPositionManagerV4);
            var v4Quoter = new V4QuoterService(web3, UniswapAddresses.BaseSepoliaQuoterV4);
            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.BaseSepoliaUniversalRouterV4);

            var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, AddressUtil.ZERO_ADDRESS);
            var amountIn = Web3.Web3.Convert.ToWei(0.001);

            var quoteExactParams = new QuoteExactParams()
            {
                Path = pathKeys,
                ExactAmount = amountIn,
                ExactCurrency = AddressUtil.ZERO_ADDRESS,
                
            };


            var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);
            var quoteAmount = Web3.Web3.Convert.FromWei(quote.AmountOut, 6); //usdc 6 decimals
           

            var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();
            var swapExactInSingle = new SwapExactIn()
            {
                AmountIn = amountIn,
                AmountOutMinimum = quote.AmountOut,
                Path = pathKeys.MapToActionV4(),
            };

            v4ActionBuilder.AddCommand(swapExactInSingle);

            var settleAllAction = new SettleAll()
            {
                Currency = AddressUtil.ZERO_ADDRESS,
                Amount = amountIn
            };

            var takeAll = new TakeAll()
            {
                Currency = usdc,
                MinAmount = 0
            };

            var routerBuilder = new UniversalRouterBuilder();
            routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

            var executeFunction = routerBuilder.GetExecuteFunction(amountIn);

            var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);

        }
    }
}
