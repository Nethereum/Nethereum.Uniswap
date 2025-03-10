# Nethereum.Uniswap V2, V3, V4, Universal Router and Permit 2

Uniswap V2, V3, V4, Universal Router and Permit 2 starter project / example integration with Nethereum.

This is a reference example, test accordingly.

## V4 Universal Router 

# Uniswap V4 Integration with Nethereum

## Setup
```csharp
var url = "https://base-sepolia.drpc.org";
var privateKey = "0xYOUR_PRIVATE_KEY";
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
```

## Quoting Prices

```csharp
var stateViewService = new StateViewService(web3, UniswapAddresses.BaseSepoliaStateViewV4);
var v4Quoter = new V4QuoterService(web3, UniswapAddresses.BaseSepoliaQuoterV4);

var pathKeys = V4PathEncoder.EncodeMultihopExactInPath(new List<PoolKey> { pool }, AddressUtil.ZERO_ADDRESS);
var amountIn = Web3.Web3.Convert.ToWei(0.001);

var quoteExactParams = new QuoteExactParams()
{
    Path = pathKeys,
    ExactAmount = amountIn,
    ExactCurrency = AddressUtil.ZERO_ADDRESS
};

var quote = await v4Quoter.QuoteExactInputQueryAsync(quoteExactParams);
var quoteAmount = Web3.Web3.Convert.FromWei(quote.AmountOut, 6); // USDC has 6 decimals
```

## Executing Swaps with Universal Router

```csharp
var universalRouter = new UniversalRouterService(web3, UniswapAddresses.BaseSepoliaUniversalRouterV4);
var v4ActionBuilder = new UniversalRouterV4ActionsBuilder();

var swapExactInSingle = new SwapExactIn()
{
    AmountIn = amountIn,
    AmountOutMinimum = quote.AmountOut,
    Path = pathKeys.MapToActionV4()
};

v4ActionBuilder.AddCommand(swapExactInSingle);

var routerBuilder = new UniversalRouterBuilder();
routerBuilder.AddCommand(v4ActionBuilder.GetV4SwapCommand());

var executeFunction = routerBuilder.GetExecuteFunction(amountIn);
var receipt = await universalRouter.ExecuteRequestAndWaitForReceiptAsync(executeFunction);
```


# Uniswap V3 / Permit 2 / V2Quoter

## Setup
```csharp
var url = "https://ethereum-sepolia.rpc.subquery.network/public";
var privateKey = "0xYOUR_PRIVATE_KEY";
var account = new Account(privateKey);
var web3 = new Nethereum.Web3.Web3(account, url);

var factoryAddress = UniswapAddresses.SepoliaUniswapV3Factory;
var permit2 = UniswapAddresses.SepoliaPermit2;
var quoterAddress = UniswapAddresses.SepoliaQuoterV2;
var universalRouter = UniswapAddresses.SepoliaUniversalRouterV3;

var uni = "0x1f9840a85d5af5bf1d1762f925bdaddc4201f984";
var weth = "0xfff9976782d46cc05630d1f6ebab18b2324d6b14";
```

## Quoting Prices
### Using Slot0 Price Calculator
```csharp
var calculator = new UniswapV3Slot0PriceCalculator(web3, factoryAddress);
var priceWethUni = await calculator.GetPoolPricesAsync(uni, weth, 500);
```

### Using Quoter V2
```csharp
var quoterService = new QuoterV2Service(web3, quoterAddress);
var weth9 = await quoterService.Weth9QueryAsync();

var amountIn = Web3.Web3.Convert.ToWei(0.001);
var abiEncoder = new Nethereum.ABI.ABIEncode();
var path = abiEncoder.GetABIEncodedPacked(
    new ABIValue("address", weth9),
    new ABIValue("uint24", 500),
    new ABIValue("address", uni));

var quote = await quoterService.QuoteExactInputQueryAsync(path, amountIn);
```

## Executing Swaps with Universal Router

### Prepare ERC20 Approval
```csharp
var weth9Service = web3.Eth.ERC20.GetContractService(weth9);
await weth9Service.ApproveRequestAndWaitForReceiptAsync(permit2, IntType.MAX_INT256_VALUE);
```

### Create and Sign Permit2
```csharp
var permit = new PermitSingle()
{
    Spender = universalRouter,
    SigDeadline = 2000000000,
    Details = new PermitDetails()
    {
        Amount = amountIn * 100000,
        Expiration = 0,
        Nonce = 0,
        Token = weth9
    }
};

var permitService = new Permit2Service(web3, permit2);
var signedPermit = await permitService.GetSinglePermitWithSignatureAsync(permit, new EthECKey(privateKey));
```

### Build and Execute Swap
```csharp
var universalRouterService = new UniversalRouterService(web3, universalRouter);
var planner = new UniversalRouterBuilder();

planner.AddCommand(new WrapEthCommand
{
    Amount = amountIn,
    Recipient = account.Address
});

planner.AddCommand(new Permit2PermitCommand
{
    Permit = signedPermit.PermitRequest,
    Signature = signedPermit.GetSignatureBytes()
});

planner.AddCommand(new V3SwapExactInCommand
{
    AmountIn = amountIn,
    AmountOutMinimum = quote.AmountOut - 10000, // slippage
    Path = path,
    Recipient = account.Address,
    FundsFromPermit2OrUniversalRouter = true
});

var receipt = await universalRouterService.ExecuteRequestAndWaitForReceiptAsync(planner.GetExecuteFunction(amountIn));
```

### Checking Balances
```csharp
var balanceWethWei = await weth9Service.BalanceOfQueryAsync(account.Address);
var balanceInEth = Web3.Web3.Convert.FromWei(balanceWethWei);

var uniService = web3.Eth.ERC20.GetContractService(uni);
var balanceUniWei = await uniService.BalanceOfQueryAsync(account.Address);
var balanceInUni = Web3.Web3.Convert.FromWei(balanceUniWei);
```

### Error Handling
```csharp
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
```

## Uniswap V2 ERC20 single path and multipath

To enable hardhat.

1. Go to the directory testchains\hardhat and run ```npm install```
2. Configure your fork alchemy api key and block number in your Test settings https://github.com/Nethereum/Nethereum.UniswapV2/blob/main/Nethereum.Uniswap.Testing/appsettings.test.json#L6
3. When you run your tests it will automatically launch hardhat and fork on the configured block number.

### Code example

```csharp
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
            
            var swapEthForExactTokens = new Contracts.UniswapV2Router02.ContractDefinition.SwapExactETHForTokensFunction()
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

            var swapTokensForExactTokens = new Contracts.UniswapV2Router02.ContractDefinition.SwapExactTokensForTokensFunction()
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

```

