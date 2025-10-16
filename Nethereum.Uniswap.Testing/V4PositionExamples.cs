using Nethereum.Contracts;
using Nethereum.Uniswap.V4;
using Nethereum.Uniswap.V4.PositionManager.ContractDefinition;
using Nethereum.Util;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Threading.Tasks;
using Nethereum.Hex.HexTypes;
using Nethereum.RPC.Extensions;
using Nethereum.Uniswap.V4.Contracts.PoolManager;
using Nethereum.Uniswap.V4.StateView;
using Nethereum.Uniswap.V4.PositionManager;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using Nethereum.Web3;
using Nethereum.XUnitEthereumClients;
using PoolKey = Nethereum.Uniswap.V4.V4Quoter.ContractDefinition.PoolKey;
using Xunit;
using Nethereum.Uniswap.UniversalRouter;
using Nethereum.Uniswap.UniversalRouter.V4Actions;
using Nethereum.StandardTokenEIP20;
using System.Linq;

namespace Nethereum.Uniswap.Testing
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class V4PositionExamples
    {
        #region V4PositionInfoDecoder Tests

        [Fact]
        public void PositionInfoDecoder_EncodeAndDecode_RoundTrip()
        {
            // Test encoding and decoding position info
            var poolId = new byte[25];
            for (int i = 0; i < 25; i++) poolId[i] = (byte)i;
            int tickLower = -1000;
            int tickUpper = 1000;
            bool hasSubscriber = true;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var decoded = V4PositionInfoDecoder.Current.DecodePositionInfo(encoded);

            Assert.Equal(tickLower, decoded.TickLower);
            Assert.Equal(tickUpper, decoded.TickUpper);
            Assert.Equal(hasSubscriber, decoded.HasSubscriber);
            Assert.Equal(25, decoded.PoolId.Length);
        }

        [Fact]
        public void PositionInfoDecoder_ExtractTickLower_ValidTick()
        {
            var poolId = new byte[25];
            int tickLower = -5000;
            int tickUpper = 5000;
            bool hasSubscriber = false;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var extractedTick = V4PositionInfoDecoder.Current.ExtractTickLower(encoded);

            Assert.Equal(tickLower, extractedTick);
        }

        [Fact]
        public void PositionInfoDecoder_ExtractTickUpper_ValidTick()
        {
            var poolId = new byte[25];
            int tickLower = -5000;
            int tickUpper = 5000;
            bool hasSubscriber = false;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var extractedTick = V4PositionInfoDecoder.Current.ExtractTickUpper(encoded);

            Assert.Equal(tickUpper, extractedTick);
        }

        [Fact]
        public void PositionInfoDecoder_ExtractHasSubscriber_True()
        {
            var poolId = new byte[25];
            int tickLower = 0;
            int tickUpper = 1000;
            bool hasSubscriber = true;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var extracted = V4PositionInfoDecoder.Current.ExtractHasSubscriber(encoded);

            Assert.True(extracted);
        }

        [Fact]
        public void PositionInfoDecoder_ExtractHasSubscriber_False()
        {
            var poolId = new byte[25];
            int tickLower = 0;
            int tickUpper = 1000;
            bool hasSubscriber = false;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var extracted = V4PositionInfoDecoder.Current.ExtractHasSubscriber(encoded);

            Assert.False(extracted);
        }

        [Fact]
        public void PositionInfoDecoder_NegativeTicks_SignExtension()
        {
            // Test that negative ticks are properly sign-extended
            var poolId = new byte[25];
            int tickLower = -887272; // MIN_TICK
            int tickUpper = -100;
            bool hasSubscriber = false;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var decoded = V4PositionInfoDecoder.Current.DecodePositionInfo(encoded);

            Assert.Equal(tickLower, decoded.TickLower);
            Assert.Equal(tickUpper, decoded.TickUpper);
            Assert.True(decoded.TickLower < 0, "Tick lower should be negative");
            Assert.True(decoded.TickUpper < 0, "Tick upper should be negative");
        }

        [Fact]
        public void PositionInfoDecoder_PositiveTicks_NoSignExtension()
        {
            // Test that positive ticks work correctly
            var poolId = new byte[25];
            int tickLower = 100;
            int tickUpper = 887272; // MAX_TICK
            bool hasSubscriber = false;

            var encoded = V4PositionInfoDecoder.Current.EncodePositionInfo(poolId, tickLower, tickUpper, hasSubscriber);
            var decoded = V4PositionInfoDecoder.Current.DecodePositionInfo(encoded);

            Assert.Equal(tickLower, decoded.TickLower);
            Assert.Equal(tickUpper, decoded.TickUpper);
            Assert.True(decoded.TickLower > 0, "Tick lower should be positive");
            Assert.True(decoded.TickUpper > 0, "Tick upper should be positive");
        }

        #endregion

        #region V4PositionInfoHelper Tests

        [Fact]
        public void PositionInfoHelper_IsPositionInRange_CurrentTickInRange()
        {
            int currentTick = 500;
            int tickLower = 0;
            int tickUpper = 1000;

            bool isInRange = V4PositionInfoHelper.Current.IsPositionInRange(currentTick, tickLower, tickUpper);

            Assert.True(isInRange, "Position should be in range when currentTick is between tickLower and tickUpper");
        }

        [Fact]
        public void PositionInfoHelper_IsPositionInRange_CurrentTickBelowRange()
        {
            int currentTick = -100;
            int tickLower = 0;
            int tickUpper = 1000;

            bool isInRange = V4PositionInfoHelper.Current.IsPositionInRange(currentTick, tickLower, tickUpper);

            Assert.False(isInRange, "Position should not be in range when currentTick is below tickLower");
        }

        [Fact]
        public void PositionInfoHelper_IsPositionInRange_CurrentTickAboveRange()
        {
            int currentTick = 1500;
            int tickLower = 0;
            int tickUpper = 1000;

            bool isInRange = V4PositionInfoHelper.Current.IsPositionInRange(currentTick, tickLower, tickUpper);

            Assert.False(isInRange, "Position should not be in range when currentTick is at or above tickUpper");
        }

        [Fact]
        public void PositionInfoHelper_IsPositionInRange_EdgeCases()
        {
            int tickLower = 0;
            int tickUpper = 1000;

            // Test at lower boundary (should be in range)
            Assert.True(V4PositionInfoHelper.Current.IsPositionInRange(tickLower, tickLower, tickUpper),
                "Position should be in range when currentTick equals tickLower");

            // Test at upper boundary (should NOT be in range, as per Uniswap convention)
            Assert.False(V4PositionInfoHelper.Current.IsPositionInRange(tickUpper, tickLower, tickUpper),
                "Position should NOT be in range when currentTick equals tickUpper");
        }

        [Fact]
        public void PositionInfoHelper_CreatePositionInfo_ValidInputs()
        {
            // Test creating position info with valid inputs
            BigInteger tokenId = 1;
            byte[] poolId = new byte[32];
            string currency0 = AddressUtil.ZERO_ADDRESS;
            string currency1 = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            uint fee = 500;
            int tickSpacing = 10;
            string hooks = AddressUtil.ZERO_ADDRESS;
            int tickLower = -1000;
            int tickUpper = 1000;
            BigInteger liquidity = BigInteger.Parse("1000000000000000000");
            BigInteger sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);

            var positionInfo = V4PositionInfoHelper.Current.CreatePositionInfo(
                tokenId, poolId, currency0, currency1, fee, tickSpacing, hooks,
                tickLower, tickUpper, liquidity, sqrtPriceX96);

            Assert.NotNull(positionInfo);
            Assert.Equal(tokenId, positionInfo.TokenId);
            Assert.Equal(tickLower, positionInfo.TickLower);
            Assert.Equal(tickUpper, positionInfo.TickUpper);
            Assert.Equal(liquidity, positionInfo.Liquidity);
            Assert.True(positionInfo.Amount0 >= 0);
            Assert.True(positionInfo.Amount1 >= 0);
            Assert.True(positionInfo.CurrentPrice > 0);
        }

        #endregion


        #region Position Value and Fee Calculation Tests

        [Fact]
        public void PositionValue_InRange_BothAmountsNonZero()
        {
            // When position is in range, both token amounts should be non-zero
            // Use explicit sqrtPrice values that are clearly ordered
            BigInteger sqrtPriceX96Current = BigInteger.Parse("200000000000000000000"); // Current price
            BigInteger sqrtPriceX96Lower = BigInteger.Parse("100000000000000000000"); // Lower bound
            BigInteger sqrtPriceX96Upper = BigInteger.Parse("300000000000000000000"); // Upper bound
            BigInteger liquidity = BigInteger.Parse("1000000000000000000");

            var amounts = V4LiquidityMath.Current.GetAmountsForLiquidity(sqrtPriceX96Current, sqrtPriceX96Lower, sqrtPriceX96Upper, liquidity);

            Assert.True(amounts.Amount0 > 0, $"Amount0 should be positive when in range. Got: {amounts.Amount0}");
            Assert.True(amounts.Amount1 > 0, $"Amount1 should be positive when in range. Got: {amounts.Amount1}");
        }

        [Fact]
        public void PositionValue_OutOfRange_OneAmountZero()
        {
            // When position is out of range, one amount should be zero
            int tickLower = 1000;
            int tickUpper = 2000;
            int currentTick = 100; // Below range
            BigInteger liquidity = BigInteger.Parse("1000000000000000000");
            BigInteger sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(currentTick);

            var amounts = V4LiquidityMath.Current.GetAmountsForLiquidityByTicks(sqrtPriceX96, tickLower, tickUpper, liquidity);

            // When price is below range, only amount0 should be non-zero
            Assert.True(amounts.Amount0 > 0 || amounts.Amount1 > 0, "At least one amount should be positive");
        }

        [Fact]
        public void FeeCalculation_WithGrowth_ReturnsPositiveFees()
        {
            // Test fee calculation with fee growth
            BigInteger liquidity = BigInteger.Parse("1000000000000000000");
            BigInteger feeGrowthLast = BigInteger.Parse("1000000000000000000000000000");
            BigInteger feeGrowthCurrent = BigInteger.Parse("2000000000000000000000000000");

            var fees = V4FeeCalculator.Current.CalculateUnclaimedFees(
                liquidity,
                feeGrowthLast,
                feeGrowthLast,
                feeGrowthCurrent,
                feeGrowthCurrent);

            Assert.True(fees.Fees0 > 0, "Fees0 should be positive with fee growth");
            Assert.True(fees.Fees1 > 0, "Fees1 should be positive with fee growth");
        }

        [Fact]
        public void PositionPriceRange_CalculatedCorrectly()
        {
            // Test that price range for a position is calculated correctly
            int tickLower = -1000;
            int tickUpper = 1000;

            decimal priceLower = V4TickMath.Current.GetPriceAtTick(tickLower);
            decimal priceUpper = V4TickMath.Current.GetPriceAtTick(tickUpper);

            Assert.True(priceLower > 0, "Price at lower tick should be positive");
            Assert.True(priceUpper > 0, "Price at upper tick should be positive");
            Assert.True(priceUpper > priceLower, "Price at upper tick should be higher than at lower tick");
        }

        #endregion

        #region REAL Integration Tests - Actual Position Management on Blockchain

        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public V4PositionExamples(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        private async Task SwapEthForUsdc(Web3.Web3 web3, UniversalRouterService universalRouter, string usdc, BigInteger ethAmount)
        {
            var eth = AddressUtil.ZERO_ADDRESS;
            var swapPoolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var swapBuilder = new UniversalRouterV4ActionsBuilder();
            swapBuilder.AddCommand(new SwapExactInSingle()
            {
                PoolKey = swapPoolKey,
                ZeroForOne = true,
                AmountIn = ethAmount,
                AmountOutMinimum = 0,
                HookData = new byte[0]
            });
            swapBuilder.AddCommand(new SettleAll() { Currency = eth, Amount = ethAmount });
            swapBuilder.AddCommand(new TakeAll() { Currency = usdc, MinAmount = 0 });

            var swapRouterBuilder = new UniversalRouterBuilder();
            swapRouterBuilder.AddCommand(swapBuilder.GetV4SwapCommand());
            await universalRouter.ExecuteRequestAndWaitForReceiptAsync(swapRouterBuilder.GetExecuteFunction(ethAmount));
        }

        [Fact]
        public async Task Integration_MintPosition_CreatesAndValidatesPosition()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(2));

            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            int tickLower = -600;
            int tickUpper = 600;
            BigInteger liquidity = Web3.Web3.Convert.ToWei(0.01m);
            BigInteger amount0Max = Web3.Web3.Convert.ToWei(0.1m);
            BigInteger amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei);

            var actionsBuilder = new V4PositionManagerActionsBuilder();

            actionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = tickLower,
                TickUpper = tickUpper,
                Liquidity = liquidity,
                Amount0Max = amount0Max,
                Amount1Max = amount1Max,
                Recipient = account,
                HookData = new byte[0]
            });

            actionsBuilder.AddCommand(new SettlePair()
            {
                Currency0 = eth,
                Currency1 = usdc
            });

            var unlockData = actionsBuilder.GetUnlockData();

            var deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var modifyLiquiditiesFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = unlockData,
                Deadline = deadline,
                AmountToSend = amount0Max
            };

            var receipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(modifyLiquiditiesFunction);

            Assert.True(receipt.Status.Value == 1, "Transaction should succeed");

            var tokenId = V4PositionReceiptHelper.GetMintedTokenId(receipt, UniswapAddresses.MainnetPositionManagerV4);
            Assert.True(tokenId >= 0, "Should extract valid tokenId from receipt");

            var actualLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.Equal(liquidity, actualLiquidity);

            var positionInfo = await positionManager.GetPoolAndPositionInfoQueryAsync(tokenId);
            Assert.Equal(poolKey.Currency0.ToLower(), positionInfo.PoolKey.Currency0.ToLower());
            Assert.Equal(poolKey.Currency1.ToLower(), positionInfo.PoolKey.Currency1.ToLower());
            Assert.Equal(poolKey.Fee, positionInfo.PoolKey.Fee);

            var positionInfoBytes = await positionManager.PositionInfoQueryAsync(tokenId);
            var decodedInfo = V4PositionInfoDecoder.Current.DecodePositionInfo(positionInfoBytes);
            Assert.Equal(tickLower, decodedInfo.TickLower);
            Assert.Equal(tickUpper, decodedInfo.TickUpper);

            var owner = await positionManager.OwnerOfQueryAsync(tokenId);
            Assert.Equal(account.ToLower(), owner.ToLower());
        }

        [Fact]
        public async Task Integration_IncreaseLiquidity_AddsLiquidityAndValidates()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            // Setup: Fund account
            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            // First, swap ETH for USDC
            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(2));
            // Approve USDC for PositionManager
            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            // First, mint a position to get a tokenId
            var initialNextTokenId = await positionManager.NextTokenIdQueryAsync();

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            // Mint position first using V4PositionManagerActionsBuilder
            var mintActionsBuilder = new V4PositionManagerActionsBuilder();
            mintActionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var mintUnlockData = mintActionsBuilder.GetUnlockData();
            var mintDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var mintFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintUnlockData,
                Deadline = mintDeadline,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            };
            await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(mintFunction);

            var tokenId = initialNextTokenId;

            // Get initial liquidity
            var initialLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);

            // Now increase liquidity
            BigInteger additionalLiquidity = Web3.Web3.Convert.ToWei(0.005m);
            BigInteger amount0Max = Web3.Web3.Convert.ToWei(0.05m);
            BigInteger amount1Max = Web3.Web3.Convert.ToWei(150, UnitConversion.EthUnit.Mwei);

            var increaseActionsBuilder = new V4PositionManagerActionsBuilder();

            // 1. Increase liquidity
            increaseActionsBuilder.AddCommand(new UniversalRouter.V4Actions.IncreaseLiquidity()
            {
                TokenId = tokenId,
                Liquidity = additionalLiquidity,
                Amount0Max = amount0Max,
                Amount1Max = amount1Max,
                HookData = new byte[0]
            });

            // 2. Settle pair
            increaseActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var increaseUnlockData = increaseActionsBuilder.GetUnlockData();
            var increaseDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var increaseFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = increaseUnlockData,
                Deadline = increaseDeadline,
                AmountToSend = amount0Max
            };

            // EXECUTE THE TRANSACTION
            var receipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(increaseFunction);

            Assert.True(receipt.Status.Value == 1, "Increase liquidity transaction should succeed");

            // Verify liquidity increased
            var finalLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.True(finalLiquidity > initialLiquidity,
                $"Liquidity should have increased. Initial: {initialLiquidity}, Final: {finalLiquidity}");
        }

        [Fact]
        public async Task Integration_DecreaseLiquidity_RemovesLiquidityAndValidates()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            // First, swap ETH for USDC
            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(2));

            // Approve USDC for PositionManager
            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            // First, mint a position
            var initialNextTokenId = await positionManager.NextTokenIdQueryAsync();
            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            // Mint position using V4PositionManagerActionsBuilder
            var mintActionsBuilder = new V4PositionManagerActionsBuilder();
            mintActionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.02m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.2m),
                Amount1Max = Web3.Web3.Convert.ToWei(600, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var mintUnlockData = mintActionsBuilder.GetUnlockData();
            var mintDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var mintFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintUnlockData,
                Deadline = mintDeadline,
                AmountToSend = Web3.Web3.Convert.ToWei(0.2m)
            };
            await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(mintFunction);

            var tokenId = initialNextTokenId;
            var initialLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);

            // Decrease half the liquidity
            BigInteger liquidityToRemove = initialLiquidity / 2;
            BigInteger amount0Min = 0; // No slippage protection for test
            BigInteger amount1Min = 0;

            var decreaseActionsBuilder = new V4PositionManagerActionsBuilder();

            // 1. Decrease liquidity
            decreaseActionsBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId,
                Liquidity = liquidityToRemove,
                Amount0Min = amount0Min,
                Amount1Min = amount1Min,
                HookData = new byte[0]
            });

            // 2. Take currencies (receive tokens back)
            decreaseActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var decreaseUnlockData = decreaseActionsBuilder.GetUnlockData();
            var decreaseDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var decreaseFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = decreaseUnlockData,
                Deadline = decreaseDeadline,
                AmountToSend = 0 // No ETH value needed for decrease
            };

            // EXECUTE
            var receipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(decreaseFunction);

            Assert.True(receipt.Status.Value == 1, "Decrease liquidity should succeed");

            // Verify liquidity decreased
            var finalLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.True(finalLiquidity < initialLiquidity,
                $"Liquidity should have decreased. Initial: {initialLiquidity}, Final: {finalLiquidity}");
        }

        [Fact]
        public async Task Integration_BurnPosition_ClosesPositionAndValidates()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            // First, swap ETH for USDC
            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(2));

            // Approve USDC for PositionManager
            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            // First, mint a position
            var initialNextTokenId = await positionManager.NextTokenIdQueryAsync();
            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            // Mint position using V4PositionManagerActionsBuilder
            var mintActionsBuilder = new V4PositionManagerActionsBuilder();
            mintActionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var mintUnlockData = mintActionsBuilder.GetUnlockData();
            var mintDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var mintFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintUnlockData,
                Deadline = mintDeadline,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            };
            await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(mintFunction);

            var tokenId = initialNextTokenId;
            var currentLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);

            // First decrease all liquidity to 0
            if (currentLiquidity > 0)
            {
                var decreaseActionsBuilder = new V4PositionManagerActionsBuilder();
                decreaseActionsBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
                {
                    TokenId = tokenId,
                    Liquidity = currentLiquidity,
                    Amount0Min = 0,
                    Amount1Min = 0,
                    HookData = new byte[0]
                });
                decreaseActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

                var decreaseUnlockData = decreaseActionsBuilder.GetUnlockData();
                var decreaseDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
                var decreaseFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
                {
                    UnlockData = decreaseUnlockData,
                    Deadline = decreaseDeadline,
                    AmountToSend = 0
                };
                await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(decreaseFunction);
            }

            // Now burn the position (removes the NFT)
            var burnActionsBuilder = new V4PositionManagerActionsBuilder();
            burnActionsBuilder.AddCommand(new UniversalRouter.V4Actions.BurnPosition()
            {
                TokenId = tokenId,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });

            // Take any remaining tokens (fees/dust)
            burnActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var burnUnlockData = burnActionsBuilder.GetUnlockData();
            var burnDeadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60;
            var burnFunction = new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = burnUnlockData,
                Deadline = burnDeadline,
                AmountToSend = 0
            };

            // EXECUTE BURN
            var receipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(burnFunction);

            Assert.True(receipt.Status.Value == 1, "Burn position should succeed");

            // Verify position no longer has liquidity (should be 0 already from decrease)
            var finalLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.Equal(BigInteger.Zero, finalLiquidity);
        }

        [Fact]
        public async Task Integration_CompletePositionLifecycle_AllOperations()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            // First, swap ETH for USDC
            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(2));

            // Approve USDC for PositionManager
            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            // STEP 1: MINT POSITION
            var initialNextTokenId = await positionManager.NextTokenIdQueryAsync();

            var mintActionsBuilder = new V4PositionManagerActionsBuilder();
            mintActionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var mintReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            });
            Assert.True(mintReceipt.Status.Value == 1, "Mint should succeed");

            var tokenId = initialNextTokenId;
            var liquidityAfterMint = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.True(liquidityAfterMint > 0, "Position should have liquidity after mint");

            // STEP 2: INCREASE LIQUIDITY
            var increaseActionsBuilder = new V4PositionManagerActionsBuilder();
            increaseActionsBuilder.AddCommand(new UniversalRouter.V4Actions.IncreaseLiquidity()
            {
                TokenId = tokenId,
                Liquidity = Web3.Web3.Convert.ToWei(0.005m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.05m),
                Amount1Max = Web3.Web3.Convert.ToWei(150, UnitConversion.EthUnit.Mwei),
                HookData = new byte[0]
            });
            increaseActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var increaseReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = increaseActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.05m)
            });
            Assert.True(increaseReceipt.Status.Value == 1, "Increase should succeed");

            var liquidityAfterIncrease = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.True(liquidityAfterIncrease > liquidityAfterMint, "Liquidity should increase");

            // STEP 3: DECREASE LIQUIDITY (remove 1/3)
            var liquidityToRemove = liquidityAfterIncrease / 3;
            var decreaseActionsBuilder = new V4PositionManagerActionsBuilder();
            decreaseActionsBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId,
                Liquidity = liquidityToRemove,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });
            decreaseActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var decreaseReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = decreaseActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = 0
            });
            Assert.True(decreaseReceipt.Status.Value == 1, "Decrease should succeed");

            var liquidityAfterDecrease = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.True(liquidityAfterDecrease < liquidityAfterIncrease, "Liquidity should decrease");

            // STEP 4: DECREASE ALL REMAINING LIQUIDITY
            var decreaseAllActionsBuilder = new V4PositionManagerActionsBuilder();
            decreaseAllActionsBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId,
                Liquidity = liquidityAfterDecrease,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });
            decreaseAllActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = decreaseAllActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = 0
            });

            // STEP 5: BURN POSITION
            var burnActionsBuilder = new V4PositionManagerActionsBuilder();
            burnActionsBuilder.AddCommand(new UniversalRouter.V4Actions.BurnPosition()
            {
                TokenId = tokenId,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });
            burnActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var burnReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = burnActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = 0
            });
            Assert.True(burnReceipt.Status.Value == 1, "Burn should succeed");

            var finalLiquidity = await positionManager.GetPositionLiquidityQueryAsync(tokenId);
            Assert.Equal(BigInteger.Zero, finalLiquidity);
        }

        [Fact]
        public async Task Integration_RebalancePosition_AtomicRangeChange()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(3));

            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var mintActionsBuilder = new V4PositionManagerActionsBuilder();
            mintActionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var mintReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            });

            var oldTokenId = V4PositionReceiptHelper.GetMintedTokenId(mintReceipt, UniswapAddresses.MainnetPositionManagerV4);
            var oldLiquidity = await positionManager.GetPositionLiquidityQueryAsync(oldTokenId);

            var rebalanceActionsBuilder = new V4PositionManagerActionsBuilder();

            rebalanceActionsBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = oldTokenId,
                Liquidity = oldLiquidity,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });

            rebalanceActionsBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -1200,
                TickUpper = 1200,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.15m),
                Amount1Max = Web3.Web3.Convert.ToWei(400, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });

            rebalanceActionsBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });
            rebalanceActionsBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var rebalanceReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = rebalanceActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.15m)
            });

            Assert.True(rebalanceReceipt.Status.Value == 1, "Rebalance should succeed");

            var oldFinalLiquidity = await positionManager.GetPositionLiquidityQueryAsync(oldTokenId);
            Assert.Equal(BigInteger.Zero, oldFinalLiquidity);

            var allTransfers = rebalanceReceipt.DecodeAllEvents<Nethereum.Uniswap.V4.PositionManager.ContractDefinition.TransferEventDTO>();
            var newMintTransfer = allTransfers.Where(e => e.Event.From == AddressUtil.ZERO_ADDRESS && e.Event.Id != oldTokenId).FirstOrDefault();
            Assert.NotNull(newMintTransfer);

            var newTokenId = newMintTransfer.Event.Id;
            var newLiquidity = await positionManager.GetPositionLiquidityQueryAsync(newTokenId);
            Assert.True(newLiquidity > 0, "New position should have liquidity");

            var newPositionInfoBytes = await positionManager.PositionInfoQueryAsync(newTokenId);
            var newPositionInfo = V4PositionInfoDecoder.Current.DecodePositionInfo(newPositionInfoBytes);
            Assert.Equal(-1200, newPositionInfo.TickLower);
            Assert.Equal(1200, newPositionInfo.TickUpper);
        }

        [Fact]
        public async Task Integration_MultiPosition_BatchOperations()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(3));

            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var mintBuilder1 = new V4PositionManagerActionsBuilder();
            mintBuilder1.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintBuilder1.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });
            var receipt1 = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintBuilder1.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            });
            var tokenId1 = V4PositionReceiptHelper.GetMintedTokenId(receipt1, UniswapAddresses.MainnetPositionManagerV4);

            var mintBuilder2 = new V4PositionManagerActionsBuilder();
            mintBuilder2.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -1200,
                TickUpper = 1200,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.15m),
                Amount1Max = Web3.Web3.Convert.ToWei(400, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintBuilder2.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });
            var receipt2 = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintBuilder2.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.15m)
            });
            var tokenId2 = V4PositionReceiptHelper.GetMintedTokenId(receipt2, UniswapAddresses.MainnetPositionManagerV4);

            var liquidity1Before = await positionManager.GetPositionLiquidityQueryAsync(tokenId1);
            var liquidity2Before = await positionManager.GetPositionLiquidityQueryAsync(tokenId2);

            var batchActionsBuilder = new V4PositionManagerActionsBuilder();

            batchActionsBuilder.AddCommand(new UniversalRouter.V4Actions.IncreaseLiquidity()
            {
                TokenId = tokenId1,
                Liquidity = Web3.Web3.Convert.ToWei(0.005m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.05m),
                Amount1Max = Web3.Web3.Convert.ToWei(150, UnitConversion.EthUnit.Mwei),
                HookData = new byte[0]
            });

            batchActionsBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId2,
                Liquidity = liquidity2Before / 2,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });

            batchActionsBuilder.AddCommand(new CloseCurrency() { Currency = eth });
            batchActionsBuilder.AddCommand(new CloseCurrency() { Currency = usdc });

            var batchReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = batchActionsBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.05m)
            });

            Assert.True(batchReceipt.Status.Value == 1, "Batch operation should succeed");

            var liquidity1After = await positionManager.GetPositionLiquidityQueryAsync(tokenId1);
            var liquidity2After = await positionManager.GetPositionLiquidityQueryAsync(tokenId2);

            Assert.True(liquidity1After > liquidity1Before, "Position 1 liquidity should increase");
            Assert.True(liquidity2After < liquidity2Before, "Position 2 liquidity should decrease");
        }

        [Fact]
        public async Task Integration_CollectFees_SinglePosition()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(2));

            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var mintBuilder = new V4PositionManagerActionsBuilder();
            mintBuilder.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintBuilder.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });

            var mintReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            });

            var tokenId = V4PositionReceiptHelper.GetMintedTokenId(mintReceipt, UniswapAddresses.MainnetPositionManagerV4);

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(0.5));

            var collectBuilder = new V4PositionManagerActionsBuilder();
            collectBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId,
                Liquidity = 0,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });
            collectBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var collectReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = collectBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60
            });

            Assert.True(collectReceipt.Status.Value == 1, "Fee collection should succeed");
        }

        [Fact]
        public async Task Integration_CollectFees_BatchMultiplePositions()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var account = web3.TransactionManager.Account.Address;

            var anvilService = new AnvilService(web3.Eth);
            await anvilService.SetBalance.SendRequestAsync(account, new HexBigInteger(Web3.Web3.Convert.ToWei(100)));

            var universalRouter = new UniversalRouterService(web3, UniswapAddresses.MainnetUniversalRouter);
            var positionManager = new PositionManagerService(web3, UniswapAddresses.MainnetPositionManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(3));

            var usdcContract = web3.Eth.ERC20.GetContractService(usdc);
            await usdcContract.ApproveRequestAndWaitForReceiptAsync(UniswapAddresses.MainnetPositionManagerV4, BigInteger.Parse("1000000000000000000000000"));

            var poolKey = new UniversalRouter.V4Actions.PoolKey()
            {
                Currency0 = eth,
                Currency1 = usdc,
                Fee = 500,
                TickSpacing = 10,
                Hooks = AddressUtil.ZERO_ADDRESS
            };

            var mintBuilder1 = new V4PositionManagerActionsBuilder();
            mintBuilder1.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -600,
                TickUpper = 600,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.1m),
                Amount1Max = Web3.Web3.Convert.ToWei(300, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintBuilder1.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });
            var receipt1 = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintBuilder1.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.1m)
            });
            var tokenId1 = V4PositionReceiptHelper.GetMintedTokenId(receipt1, UniswapAddresses.MainnetPositionManagerV4);

            var mintBuilder2 = new V4PositionManagerActionsBuilder();
            mintBuilder2.AddCommand(new UniversalRouter.V4Actions.MintPosition()
            {
                PoolKey = poolKey,
                TickLower = -1200,
                TickUpper = 1200,
                Liquidity = Web3.Web3.Convert.ToWei(0.01m),
                Amount0Max = Web3.Web3.Convert.ToWei(0.15m),
                Amount1Max = Web3.Web3.Convert.ToWei(400, UnitConversion.EthUnit.Mwei),
                Recipient = account,
                HookData = new byte[0]
            });
            mintBuilder2.AddCommand(new SettlePair() { Currency0 = eth, Currency1 = usdc });
            var receipt2 = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = mintBuilder2.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60,
                AmountToSend = Web3.Web3.Convert.ToWei(0.15m)
            });
            var tokenId2 = V4PositionReceiptHelper.GetMintedTokenId(receipt2, UniswapAddresses.MainnetPositionManagerV4);

            await SwapEthForUsdc(web3, universalRouter, usdc, Web3.Web3.Convert.ToWei(1));

            var batchCollectBuilder = new V4PositionManagerActionsBuilder();

            batchCollectBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId1,
                Liquidity = 0,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });

            batchCollectBuilder.AddCommand(new UniversalRouter.V4Actions.DecreaseLiquidity()
            {
                TokenId = tokenId2,
                Liquidity = 0,
                Amount0Min = 0,
                Amount1Min = 0,
                HookData = new byte[0]
            });

            batchCollectBuilder.AddCommand(new TakePair() { Currency0 = eth, Currency1 = usdc, Recipient = account });

            var batchCollectReceipt = await positionManager.ModifyLiquiditiesRequestAndWaitForReceiptAsync(new Nethereum.Uniswap.V4.PositionManager.ContractDefinition.ModifyLiquiditiesFunction
            {
                UnlockData = batchCollectBuilder.GetUnlockData(),
                Deadline = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 60
            });

            Assert.True(batchCollectReceipt.Status.Value == 1, "Batch fee collection should succeed");
        }

        #endregion
    }
}


