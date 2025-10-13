using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Xunit;
using Nethereum.Uniswap.V4;

namespace Nethereum.Uniswap.Testing
{
    public class V4BestPathFinderUnitTests
    {
        [Fact]
        public void EnumerateTokenRoutes_RespectsMaxHops()
        {
            var method = typeof(V4BestPathFinder).GetMethod(
                "EnumerateTokenRoutes",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            var arguments = new object[]
            {
                "TokenA",
                "TokenD",
                new[] { "TokenB", "TokenC" },
                2
            };

            var routesWithTwoHops = ((IEnumerable)method.Invoke(null, arguments))
                .Cast<List<string>>()
                .Select(route => string.Join("->", route))
                .ToList();

            Assert.Contains("TokenA->TokenD", routesWithTwoHops);
            Assert.DoesNotContain("TokenA->TokenB->TokenC->TokenD", routesWithTwoHops);

            arguments[3] = 3;

            var routesWithThreeHops = ((IEnumerable)method.Invoke(null, arguments))
                .Cast<List<string>>()
                .Select(route => string.Join("->", route))
                .ToList();

            Assert.Contains("TokenA->TokenB->TokenC->TokenD", routesWithThreeHops);
        }

        [Fact]
        public void BuildCandidateLookup_NormalizesTokenPairs()
        {
            var method = typeof(V4BestPathFinder).GetMethod(
                "BuildCandidateLookup",
                BindingFlags.NonPublic | BindingFlags.Static);

            Assert.NotNull(method);

            var pools = new List<PoolCacheEntry>
            {
                new PoolCacheEntry
                {
                    Currency0 = "0xaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa",
                    Currency1 = "0xbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbbb",
                    Fee = 500,
                    TickSpacing = 10,
                    Exists = true
                },
                new PoolCacheEntry
                {
                    Currency0 = "0xBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBBB",
                    Currency1 = "0xAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA",
                    Fee = 3000,
                    TickSpacing = 60,
                    Exists = true
                }
            };

            var lookup = (Dictionary<string, List<PoolCacheEntry>>)method.Invoke(null, new object[] { pools });

            Assert.Single(lookup);
            Assert.Equal(2, lookup.First().Value.Count);
        }
    }
}
