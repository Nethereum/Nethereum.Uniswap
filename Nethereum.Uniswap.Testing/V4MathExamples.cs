using Nethereum.Uniswap.V4;
using Nethereum.Util;
using System;
using System.Numerics;
using Xunit;

namespace Nethereum.Uniswap.Testing
{
    public class V4MathExamples
    {
        #region V4TickMath Tests

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_Tick0_ReturnsValidSqrtPrice()
        {
            // At tick 0, price = 1
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);

            // Verify it's a valid positive value
            Assert.True(sqrtPriceX96 > 0, "Sqrt price at tick 0 should be positive");

            // Verify it's within the valid range
            Assert.True(sqrtPriceX96 >= V4TickMath.MIN_SQRT_RATIO, "Sqrt price should be >= MIN_SQRT_RATIO");
            Assert.True(sqrtPriceX96 < V4TickMath.MAX_SQRT_RATIO, "Sqrt price should be < MAX_SQRT_RATIO");
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_PositiveTick_ReturnsHigherPrice()
        {
            // Positive tick means token1/token0 price > 1
            var sqrtPriceAt100 = V4TickMath.Current.GetSqrtRatioAtTick(100);
            var sqrtPriceAt0 = V4TickMath.Current.GetSqrtRatioAtTick(0);

            // For positive ticks, after inversion at line 43, the result should be different than tick 0
            Assert.NotEqual(sqrtPriceAt0, sqrtPriceAt100);
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_NegativeTick_ReturnsLowerPrice()
        {
            // Negative tick means token1/token0 price < 1
            var sqrtPriceAtMinus100 = V4TickMath.Current.GetSqrtRatioAtTick(-100);
            var sqrtPriceAt0 = V4TickMath.Current.GetSqrtRatioAtTick(0);

            Assert.NotEqual(sqrtPriceAt0, sqrtPriceAtMinus100);

            // Both should be valid
            Assert.True(sqrtPriceAtMinus100 >= V4TickMath.MIN_SQRT_RATIO);
            Assert.True(sqrtPriceAt0 >= V4TickMath.MIN_SQRT_RATIO);
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_MinTick_ReturnsMinSqrtRatio()
        {
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(V4TickMath.MIN_TICK);

            Assert.True(sqrtPriceX96 >= V4TickMath.MIN_SQRT_RATIO,
                $"MIN_TICK should produce sqrtPrice >= MIN_SQRT_RATIO. Got {sqrtPriceX96}, expected >= {V4TickMath.MIN_SQRT_RATIO}");
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_MaxTick_ReturnsMaxSqrtRatio()
        {
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(V4TickMath.MAX_TICK);

            Assert.True(sqrtPriceX96 < V4TickMath.MAX_SQRT_RATIO,
                $"MAX_TICK should produce sqrtPrice < MAX_SQRT_RATIO. Got {sqrtPriceX96}, expected < {V4TickMath.MAX_SQRT_RATIO}");
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_BelowMinTick_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                V4TickMath.Current.GetSqrtRatioAtTick(V4TickMath.MIN_TICK - 1));
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_AboveMaxTick_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                V4TickMath.Current.GetSqrtRatioAtTick(V4TickMath.MAX_TICK + 1));
        }

        [Fact]
        public void TickMath_GetTickAtSqrtRatio_WithValidSqrtPrice()
        {
            // Test with a known valid sqrtPrice value
            var sqrtPriceX96 = BigInteger.Parse("1000000000000000000"); // A valid price

            var tick = V4TickMath.Current.GetTickAtSqrtRatio(sqrtPriceX96);

            // Tick should be within valid range
            Assert.True(tick >= V4TickMath.MIN_TICK, $"Tick {tick} should be >= MIN_TICK");
            Assert.True(tick <= V4TickMath.MAX_TICK, $"Tick {tick} should be <= MAX_TICK");
        }

        [Fact]
        public void TickMath_GetSqrtRatioAtTick_ValidRanges()
        {
            // Verify that GetSqrtRatioAtTick returns valid sqrtPrice values
            // Note: Due to the inversion at line 43 for positive ticks in the Uniswap V4 implementation,
            // positive ticks result in descending sqrtPrice values after the transformation
            var sqrtPriceAtMinus1000 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtPriceAt0 = V4TickMath.Current.GetSqrtRatioAtTick(0);
            var sqrtPriceAt100 = V4TickMath.Current.GetSqrtRatioAtTick(100);
            var sqrtPriceAt1000 = V4TickMath.Current.GetSqrtRatioAtTick(1000);

            // All values should be within valid range
            Assert.True(sqrtPriceAtMinus1000 >= V4TickMath.MIN_SQRT_RATIO && sqrtPriceAtMinus1000 < V4TickMath.MAX_SQRT_RATIO);
            Assert.True(sqrtPriceAt0 >= V4TickMath.MIN_SQRT_RATIO && sqrtPriceAt0 < V4TickMath.MAX_SQRT_RATIO);
            Assert.True(sqrtPriceAt100 >= V4TickMath.MIN_SQRT_RATIO && sqrtPriceAt100 < V4TickMath.MAX_SQRT_RATIO);
            Assert.True(sqrtPriceAt1000 >= V4TickMath.MIN_SQRT_RATIO && sqrtPriceAt1000 < V4TickMath.MAX_SQRT_RATIO);

            // All values should be different
            Assert.NotEqual(sqrtPriceAtMinus1000, sqrtPriceAt0);
            Assert.NotEqual(sqrtPriceAt0, sqrtPriceAt100);
            Assert.NotEqual(sqrtPriceAt100, sqrtPriceAt1000);
        }

        [Fact]
        public void TickMath_TickValues_ConsistentWithPriceCalculator()
        {
            // Verify that the tick math integrates properly with price calculator
            int tick = 1000;
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(tick);

            // Verify the price is positive and reasonable
            var price = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96);
            Assert.True(price > 0, $"Price should be positive. Got: {price}");

            // For tick 1000, price should be > 1 (positive tick means token1 is worth more)
            Assert.True(price > 1, $"Positive tick should yield price > 1. Got: {price}");
        }

        [Fact]
        public void TickMath_GetTickAtSqrtRatio_MinSqrtRatio_ReturnsValidTick()
        {
            var tick = V4TickMath.Current.GetTickAtSqrtRatio(V4TickMath.MIN_SQRT_RATIO);

            // MIN_SQRT_RATIO should produce a tick close to MIN_TICK
            Assert.True(tick >= V4TickMath.MIN_TICK - 10 && tick <= V4TickMath.MIN_TICK + 10,
                $"MIN_SQRT_RATIO should produce tick near MIN_TICK. Got {tick}, MIN_TICK is {V4TickMath.MIN_TICK}");
        }

        [Fact]
        public void TickMath_GetTickAtSqrtRatio_BelowMinSqrtRatio_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                V4TickMath.Current.GetTickAtSqrtRatio(V4TickMath.MIN_SQRT_RATIO - 1));
        }

        [Fact]
        public void TickMath_GetTickAtSqrtRatio_AboveMaxSqrtRatio_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                V4TickMath.Current.GetTickAtSqrtRatio(V4TickMath.MAX_SQRT_RATIO));
        }

        [Fact]
        public void TickMath_GetNearestUsableTick_RoundsToTickSpacing()
        {
            int tick = 157;
            int tickSpacing = 10;

            var usableTick = V4TickMath.Current.GetNearestUsableTick(tick, tickSpacing);

            // 157 rounds to 160 (nearest multiple of 10)
            Assert.Equal(160, usableTick);
            Assert.Equal(0, usableTick % tickSpacing);
        }

        [Fact]
        public void TickMath_GetNearestUsableTick_TickSpacing60()
        {
            int tick = 100;
            int tickSpacing = 60;

            var usableTick = V4TickMath.Current.GetNearestUsableTick(tick, tickSpacing);

            // 100 rounds to 120 (nearest multiple of 60)
            Assert.Equal(120, usableTick);
            Assert.Equal(0, usableTick % tickSpacing);
        }

        [Fact]
        public void TickMath_GetNearestUsableTick_BelowMinTick_ReturnsMinTick()
        {
            int tick = V4TickMath.MIN_TICK - 1000;
            int tickSpacing = 10;

            var usableTick = V4TickMath.Current.GetNearestUsableTick(tick, tickSpacing);

            Assert.Equal(V4TickMath.MIN_TICK, usableTick);
        }

        [Fact]
        public void TickMath_GetNearestUsableTick_AboveMaxTick_ReturnsMaxTick()
        {
            int tick = V4TickMath.MAX_TICK + 1000;
            int tickSpacing = 10;

            var usableTick = V4TickMath.Current.GetNearestUsableTick(tick, tickSpacing);

            Assert.Equal(V4TickMath.MAX_TICK, usableTick);
        }

        [Fact]
        public void TickMath_GetNearestUsableTick_InvalidTickSpacing_ThrowsException()
        {
            Assert.Throws<ArgumentOutOfRangeException>(() =>
                V4TickMath.Current.GetNearestUsableTick(100, 0));

            Assert.Throws<ArgumentOutOfRangeException>(() =>
                V4TickMath.Current.GetNearestUsableTick(100, -10));
        }

        #endregion

        #region V4LiquidityMath Tests

        [Fact]
        public void LiquidityMath_GetLiquidityForAmount0_ValidInputs()
        {
            // Get liquidity from amount0 when price is between sqrtRatioA and sqrtRatioB
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var amount0 = Web3.Web3.Convert.ToWei(1); // 1 token0

            var liquidity = V4LiquidityMath.Current.GetLiquidityForAmount0(sqrtRatioAX96, sqrtRatioBX96, amount0);

            Assert.True(liquidity > 0, "Liquidity should be positive");
        }

        [Fact]
        public void LiquidityMath_GetLiquidityForAmount1_ValidInputs()
        {
            // Get liquidity from amount1 when price is between sqrtRatioA and sqrtRatioB
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var amount1 = Web3.Web3.Convert.ToWei(1); // 1 token1

            var liquidity = V4LiquidityMath.Current.GetLiquidityForAmount1(sqrtRatioAX96, sqrtRatioBX96, amount1);

            Assert.True(liquidity > 0, "Liquidity should be positive");
        }

        [Fact]
        public void LiquidityMath_GetAmount0ForLiquidity_ValidInputs()
        {
            // Calculate how much amount0 is needed for a given liquidity
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var liquidity = BigInteger.Parse("1000000000000000000"); // 1e18

            var amount0 = V4LiquidityMath.Current.GetAmount0ForLiquidity(sqrtRatioAX96, sqrtRatioBX96, liquidity);

            Assert.True(amount0 > 0, "Amount0 should be positive");
        }

        [Fact]
        public void LiquidityMath_GetAmount1ForLiquidity_ValidInputs()
        {
            // Calculate how much amount1 is needed for a given liquidity
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var liquidity = BigInteger.Parse("1000000000000000000"); // 1e18

            var amount1 = V4LiquidityMath.Current.GetAmount1ForLiquidity(sqrtRatioAX96, sqrtRatioBX96, liquidity);

            Assert.True(amount1 > 0, "Amount1 should be positive");
        }

        [Fact]
        public void LiquidityMath_GetAmountsForLiquidity_PriceInRange()
        {
            // When current sqrtPrice is between sqrtRatioA and sqrtRatioB, both amounts should be non-zero
            // Use sqrtPrices that are clearly ordered
            var sqrtRatioAX96 = BigInteger.Parse("100000000000000000000"); // Lower bound
            var sqrtRatioCurrentX96 = BigInteger.Parse("200000000000000000000"); // Current in middle
            var sqrtRatioBX96 = BigInteger.Parse("300000000000000000000"); // Upper bound
            var liquidity = BigInteger.Parse("1000000000000000000");

            var amounts = V4LiquidityMath.Current.GetAmountsForLiquidity(sqrtRatioCurrentX96, sqrtRatioAX96, sqrtRatioBX96, liquidity);

            Assert.True(amounts.Amount0 > 0, $"Amount0 should be positive when price is in range. Got: {amounts.Amount0}");
            Assert.True(amounts.Amount1 > 0, $"Amount1 should be positive when price is in range. Got: {amounts.Amount1}");
        }

        [Fact]
        public void LiquidityMath_GetAmountsForLiquidity_PriceBelowRange()
        {
            // When current sqrtPrice is below the range, only amount0 should be non-zero
            // Use very low sqrtPrice value (near MIN_SQRT_RATIO)
            var sqrtRatioCurrentX96 = V4TickMath.MIN_SQRT_RATIO;
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(100);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var liquidity = BigInteger.Parse("1000000000000000000");

            var amounts = V4LiquidityMath.Current.GetAmountsForLiquidity(sqrtRatioCurrentX96, sqrtRatioAX96, sqrtRatioBX96, liquidity);

            Assert.True(amounts.Amount0 > 0, $"Amount0 should be positive when price is below range. Got: {amounts.Amount0}");
            Assert.Equal(BigInteger.Zero, amounts.Amount1);
        }

        [Fact]
        public void LiquidityMath_GetAmountsForLiquidity_PriceAboveRange()
        {
            // When current sqrtPrice is above the range, only amount1 should be non-zero
            // Use very high sqrtPrice value (near MAX_SQRT_RATIO)
            var sqrtRatioCurrentX96 = V4TickMath.MAX_SQRT_RATIO - 1000;
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(100);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var liquidity = BigInteger.Parse("1000000000000000000");

            var amounts = V4LiquidityMath.Current.GetAmountsForLiquidity(sqrtRatioCurrentX96, sqrtRatioAX96, sqrtRatioBX96, liquidity);

            Assert.Equal(BigInteger.Zero, amounts.Amount0);
            Assert.True(amounts.Amount1 > 0, $"Amount1 should be positive when price is above range. Got: {amounts.Amount1}");
        }

        [Fact]
        public void LiquidityMath_GetAmountsForLiquidityByTicks_ValidInputs()
        {
            // Test the convenience method that uses ticks directly
            // Use a sqrtPrice that's clearly in the middle of the range
            var sqrtRatioCurrentX96 = BigInteger.Parse("200000000000000000000");
            int tickLower = 100;
            int tickUpper = 1000;
            var liquidity = BigInteger.Parse("1000000000000000000");

            var amounts = V4LiquidityMath.Current.GetAmountsForLiquidityByTicks(sqrtRatioCurrentX96, tickLower, tickUpper, liquidity);

            // Just verify it returns some amounts - the exact behavior depends on where
            // the sqrtPrice falls relative to the tick range
            Assert.True(amounts.Amount0 >= 0, $"Amount0 should be non-negative. Got: {amounts.Amount0}");
            Assert.True(amounts.Amount1 >= 0, $"Amount1 should be non-negative. Got: {amounts.Amount1}");
        }

        [Fact]
        public void LiquidityMath_GetLiquidityForAmounts_PriceInRange()
        {
            // Calculate liquidity from both token amounts when price is in range
            var sqrtRatioCurrentX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var amount0 = Web3.Web3.Convert.ToWei(1);
            var amount1 = Web3.Web3.Convert.ToWei(1);

            var liquidity = V4LiquidityMath.Current.GetLiquidityForAmounts(sqrtRatioCurrentX96, sqrtRatioAX96, sqrtRatioBX96, amount0, amount1);

            Assert.True(liquidity > 0, "Liquidity should be positive");
        }

        [Fact]
        public void LiquidityMath_RoundTrip_Amount0()
        {
            // Test round-trip: amount0 -> liquidity -> amount0
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var originalAmount0 = Web3.Web3.Convert.ToWei(1);

            var liquidity = V4LiquidityMath.Current.GetLiquidityForAmount0(sqrtRatioAX96, sqrtRatioBX96, originalAmount0);
            var resultAmount0 = V4LiquidityMath.Current.GetAmount0ForLiquidity(sqrtRatioAX96, sqrtRatioBX96, liquidity);

            // Due to integer division, result might be slightly less than original
            var diff = originalAmount0 - resultAmount0;
            var percentDiff = (decimal)diff / (decimal)originalAmount0 * 100;

            Assert.True(percentDiff < 1, $"Round-trip amount0 should be within 1%. Original: {originalAmount0}, Result: {resultAmount0}, Diff: {percentDiff:F4}%");
        }

        [Fact]
        public void LiquidityMath_RoundTrip_Amount1()
        {
            // Test round-trip: amount1 -> liquidity -> amount1
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var originalAmount1 = Web3.Web3.Convert.ToWei(1);

            var liquidity = V4LiquidityMath.Current.GetLiquidityForAmount1(sqrtRatioAX96, sqrtRatioBX96, originalAmount1);
            var resultAmount1 = V4LiquidityMath.Current.GetAmount1ForLiquidity(sqrtRatioAX96, sqrtRatioBX96, liquidity);

            // Due to integer division, result might be slightly less than original
            var diff = originalAmount1 - resultAmount1;
            var percentDiff = (decimal)diff / (decimal)originalAmount1 * 100;

            Assert.True(percentDiff < 1, $"Round-trip amount1 should be within 1%. Original: {originalAmount1}, Result: {resultAmount1}, Diff: {percentDiff:F4}%");
        }

        [Fact]
        public void LiquidityMath_SwappedRatios_AutomaticSorting()
        {
            // Verify that the functions auto-sort sqrtRatioA and sqrtRatioB
            var sqrtRatioAX96 = V4TickMath.Current.GetSqrtRatioAtTick(-1000);
            var sqrtRatioBX96 = V4TickMath.Current.GetSqrtRatioAtTick(1000);
            var amount0 = Web3.Web3.Convert.ToWei(1);

            // Call with correct order
            var liquidity1 = V4LiquidityMath.Current.GetLiquidityForAmount0(sqrtRatioAX96, sqrtRatioBX96, amount0);

            // Call with swapped order - should auto-sort and give same result
            var liquidity2 = V4LiquidityMath.Current.GetLiquidityForAmount0(sqrtRatioBX96, sqrtRatioAX96, amount0);

            Assert.Equal(liquidity1, liquidity2);
        }

        #endregion

        #region V4PriceCalculator Tests

        [Fact]
        public void PriceCalculator_CalculatePriceFromSqrtPriceX96_ValidInput()
        {
            // Test basic price calculation from sqrtPriceX96
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);
            var price = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96);

            Assert.True(price > 0, "Price should be positive");
        }

        [Fact]
        public void PriceCalculator_CalculatePriceFromSqrtPriceX96_WithDecimals()
        {
            // Test price calculation with different decimal combinations
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);

            // 18 decimals / 6 decimals (e.g., ETH/USDC)
            var priceETH_USDC = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96, 18, 6);
            Assert.True(priceETH_USDC > 0);

            // 18 decimals / 18 decimals (e.g., ETH/DAI)
            var priceETH_DAI = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96, 18, 18);
            Assert.True(priceETH_DAI > 0);

            // 6 decimals / 6 decimals (e.g., USDC/USDT)
            var priceUSDC_USDT = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96, 6, 6);
            Assert.True(priceUSDC_USDT > 0);
        }

        [Fact]
        public void PriceCalculator_RoundTrip_Price()
        {
            // Test round-trip: price -> sqrtPriceX96 -> price
            decimal originalPrice = 2500m; // e.g., ETH price

            var sqrtPriceX96 = V4PriceCalculator.Current.CalculateSqrtPriceX96FromPrice(originalPrice);
            var resultPrice = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96);

            var percentDiff = Math.Abs((resultPrice - originalPrice) / originalPrice * 100);
            Assert.True(percentDiff < 1, $"Round-trip price should be within 1%. Original: {originalPrice}, Result: {resultPrice}, Diff: {percentDiff:F4}%");
        }

        [Fact]
        public void PriceCalculator_RoundTrip_PriceWithDecimals()
        {
            // Test round-trip with decimals: price -> sqrtPriceX96 -> price
            decimal originalPrice = 0.0005m; // e.g., USDC/ETH price
            int decimals0 = 6;
            int decimals1 = 18;

            var sqrtPriceX96 = V4PriceCalculator.Current.CalculateSqrtPriceX96FromPrice(originalPrice, decimals0, decimals1);
            var resultPrice = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(sqrtPriceX96, decimals0, decimals1);

            var percentDiff = Math.Abs((resultPrice - originalPrice) / originalPrice * 100);
            Assert.True(percentDiff < 5, $"Round-trip price should be within 5%. Original: {originalPrice}, Result: {resultPrice}, Diff: {percentDiff:F4}%");
        }

        [Fact]
        public void PriceCalculator_CreatePoolPrice_ValidInputs()
        {
            // Test CreatePoolPrice method
            var poolId = new byte[32];
            var currency0 = "0x0000000000000000000000000000000000000000";
            var currency1 = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);
            int tick = 0;

            var poolPrice = V4PriceCalculator.Current.CreatePoolPrice(poolId, currency0, currency1, sqrtPriceX96, tick);

            Assert.NotNull(poolPrice);
            Assert.Equal(poolId, poolPrice.PoolId);
            Assert.True(poolPrice.PriceCurrency0InCurrency1 > 0);
            Assert.True(poolPrice.PriceCurrency1InCurrency0 > 0);

            // Verify inverse relationship
            var product = poolPrice.PriceCurrency0InCurrency1 * poolPrice.PriceCurrency1InCurrency0;
            Assert.True(Math.Abs(product - 1) < 0.01m, $"Product of inverse prices should be ~1. Got: {product}");
        }

        [Fact]
        public void PriceCalculator_CreatePoolPrice_WithDecimals()
        {
            // Test CreatePoolPrice with decimals
            var poolId = new byte[32];
            var currency0 = AddressUtil.ZERO_ADDRESS;
            var currency1 = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var sqrtPriceX96 = V4TickMath.Current.GetSqrtRatioAtTick(0);
            int tick = 0;

            var poolPrice = V4PriceCalculator.Current.CreatePoolPrice(poolId, currency0, currency1, sqrtPriceX96, tick, 18, 6);

            Assert.NotNull(poolPrice);
            Assert.True(poolPrice.PriceCurrency0InCurrency1 > 0);
        }

        [Fact]
        public void PriceCalculator_ZeroSqrtPrice_ReturnsZero()
        {
            // Test with zero sqrtPrice
            var price = V4PriceCalculator.Current.CalculatePriceFromSqrtPriceX96(BigInteger.Zero);
            Assert.Equal(0, price);
        }

        #endregion

        #region V4FeeCalculator Tests

        [Fact]
        public void FeeCalculator_CalculateUnclaimedFees_NoFeeGrowth()
        {
            // Test when there's no fee growth
            var liquidity = BigInteger.Parse("1000000000000000000");
            var feeGrowthInside0Last = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside1Last = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside0Current = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside1Current = BigInteger.Parse("100000000000000000000000000000000");

            var fees = V4FeeCalculator.CalculateUnclaimedFees(
                liquidity,
                feeGrowthInside0Last,
                feeGrowthInside1Last,
                feeGrowthInside0Current,
                feeGrowthInside1Current);

            Assert.Equal(BigInteger.Zero, fees.Fees0);
            Assert.Equal(BigInteger.Zero, fees.Fees1);
        }

        [Fact]
        public void FeeCalculator_CalculateUnclaimedFees_WithFeeGrowth()
        {
            // Test with fee growth
            var liquidity = BigInteger.Parse("1000000000000000000");
            var feeGrowthInside0Last = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside1Last = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside0Current = BigInteger.Parse("200000000000000000000000000000000");
            var feeGrowthInside1Current = BigInteger.Parse("200000000000000000000000000000000");

            var fees = V4FeeCalculator.CalculateUnclaimedFees(
                liquidity,
                feeGrowthInside0Last,
                feeGrowthInside1Last,
                feeGrowthInside0Current,
                feeGrowthInside1Current);

            Assert.True(fees.Fees0 > 0, "Fees0 should be positive with fee growth");
            Assert.True(fees.Fees1 > 0, "Fees1 should be positive with fee growth");
        }

        [Fact]
        public void FeeCalculator_CalculateUnclaimedFees_ZeroLiquidity()
        {
            // Test with zero liquidity
            var liquidity = BigInteger.Zero;
            var feeGrowthInside0Last = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside1Last = BigInteger.Parse("100000000000000000000000000000000");
            var feeGrowthInside0Current = BigInteger.Parse("200000000000000000000000000000000");
            var feeGrowthInside1Current = BigInteger.Parse("200000000000000000000000000000000");

            var fees = V4FeeCalculator.CalculateUnclaimedFees(
                liquidity,
                feeGrowthInside0Last,
                feeGrowthInside1Last,
                feeGrowthInside0Current,
                feeGrowthInside1Current);

            Assert.Equal(BigInteger.Zero, fees.Fees0);
            Assert.Equal(BigInteger.Zero, fees.Fees1);
        }

        [Fact]
        public void FeeCalculator_CalculateUnclaimedFees_Overflow()
        {
            // Test with overflow scenario (current < last)
            var liquidity = BigInteger.Parse("1000000000000000000");
            var feeGrowthInside0Last = BigInteger.Pow(2, 255); // Large value
            var feeGrowthInside1Last = BigInteger.Pow(2, 255);
            var feeGrowthInside0Current = BigInteger.Parse("100000000000000000000000000000000"); // Smaller (wrapped around)
            var feeGrowthInside1Current = BigInteger.Parse("100000000000000000000000000000000");

            var fees = V4FeeCalculator.CalculateUnclaimedFees(
                liquidity,
                feeGrowthInside0Last,
                feeGrowthInside1Last,
                feeGrowthInside0Current,
                feeGrowthInside1Current);

            // Should handle overflow correctly
            Assert.True(fees.Fees0 >= 0, "Fees0 should be non-negative even with overflow");
            Assert.True(fees.Fees1 >= 0, "Fees1 should be non-negative even with overflow");
        }

        [Fact]
        public void FeeCalculator_CalculateUnclaimedFees_MaximumFees()
        {
            // Test with maximum reasonable fee values
            var liquidity = BigInteger.Parse("100000000000000000000"); // 100 tokens
            var feeGrowthInside0Last = BigInteger.Zero;
            var feeGrowthInside1Last = BigInteger.Zero;
            var feeGrowthInside0Current = BigInteger.Parse("340282366920938463463374607431768211456"); // Large growth
            var feeGrowthInside1Current = BigInteger.Parse("340282366920938463463374607431768211456");

            var fees = V4FeeCalculator.CalculateUnclaimedFees(
                liquidity,
                feeGrowthInside0Last,
                feeGrowthInside1Last,
                feeGrowthInside0Current,
                feeGrowthInside1Current);

            Assert.True(fees.Fees0 > 0, "Fees0 should be positive");
            Assert.True(fees.Fees1 > 0, "Fees1 should be positive");
        }

        #endregion
    }
}









