using System.Numerics;
using System.Reflection;
using Nethereum.Uniswap.V4;
using Xunit;

namespace Nethereum.Uniswap.Testing
{
    public class V4PriceServiceTests
    {
        [Fact]
        public void CalculatePrice_DoesNotOverflowForLargeAmounts()
        {
            var method = typeof(V4PriceService).GetMethod("CalculatePrice", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var largeAmount = BigInteger.Pow(10, 120);

            var safeFromWei = typeof(V4PriceService).GetMethod("SafeFromWei", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(safeFromWei);
            _ = safeFromWei.Invoke(null, new object[] { largeAmount, 18 });

            var result = (decimal)method.Invoke(null, new object[]
            {
                largeAmount,
                largeAmount,
                18,
                18
            });

            Assert.True(result > 0);
        }

        [Fact]
        public void SelectBetterPrice_PrefersHigherValidPrice()
        {
            var method = typeof(V4PriceService).GetMethod("SelectBetterPrice", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var currentBest = new TokenPrice { Price = 100m, IsValid = true };
            var candidate = new TokenPrice { Price = 150m, IsValid = true };

            var result = (TokenPrice)method.Invoke(null, new object[] { currentBest, candidate });

            Assert.Same(candidate, result);
        }

        [Fact]
        public void SelectBetterPrice_KeepsCurrentWhenCandidateInvalid()
        {
            var method = typeof(V4PriceService).GetMethod("SelectBetterPrice", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(method);

            var currentBest = new TokenPrice { Price = 100m, IsValid = true };
            var candidate = new TokenPrice { Price = 1000m, IsValid = false };

            var result = (TokenPrice)method.Invoke(null, new object[] { currentBest, candidate });

            Assert.Same(currentBest, result);
        }
    }
}
