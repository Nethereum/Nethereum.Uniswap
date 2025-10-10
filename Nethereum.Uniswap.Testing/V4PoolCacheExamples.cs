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

namespace Nethereum.Uniswap.Testing
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class V4PoolCacheExamples
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;

        public V4PoolCacheExamples(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }

        [Fact]
        public async Task TestPoolCacheGetOrFetch()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(web3, UniswapAddresses.MainnetStateViewV4);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            var pool1 = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            Assert.NotNull(pool1);
            Assert.NotNull(pool1.PoolId);

            var pool2 = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            Assert.Equal(pool1.PoolId, pool2.PoolId);
            Assert.Equal(pool1.LastUpdated, pool2.LastUpdated);
        }

        [Fact]
        public async Task TestPoolCacheFindPoolsForPair()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(web3, UniswapAddresses.MainnetStateViewV4);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            var pools = await poolCache.FindPoolsForPairAsync(eth, usdc, new int[] { 500, 3000 }, new int[] { 10, 60 });
            Assert.NotNull(pools);
        }

        [Fact]
        public async Task TestPoolCacheExpiration()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(
                web3,
                UniswapAddresses.MainnetStateViewV4,
                cacheExpiration: TimeSpan.FromSeconds(1));

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            var pool1 = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            var firstUpdate = pool1.LastUpdated;

            await Task.Delay(1100);

            var pool2 = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            Assert.True(pool2.LastUpdated > firstUpdate);
        }

        [Fact]
        public async Task TestPoolCacheRefresh()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(web3, UniswapAddresses.MainnetStateViewV4);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            var pool1 = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            var firstUpdate = pool1.LastUpdated;

            await Task.Delay(100);

            var pool2 = await poolCache.RefreshPoolAsync(pool1.PoolId);
            Assert.NotNull(pool2);
            Assert.True(pool2.LastUpdated > firstUpdate);
        }

        [Fact]
        public async Task TestPoolCacheClearAndGetAll()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(web3, UniswapAddresses.MainnetStateViewV4);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            await poolCache.GetOrFetchPoolAsync(eth, usdc, 3000, 60);

            var allPools = await poolCache.GetAllCachedPoolsAsync();
            Assert.True(allPools.Count >= 2);

            await poolCache.ClearCacheAsync();

            allPools = await poolCache.GetAllCachedPoolsAsync();
            Assert.Empty(allPools);
        }

        [Fact]
        public async Task TestFindPoolsForTokenUsingCache()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(
                web3,
                UniswapAddresses.MainnetStateViewV4,
                UniswapAddresses.MainnetPoolManagerV4);

            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";
            var eth = AddressUtil.ZERO_ADDRESS;

            var pool = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            Assert.NotNull(pool);

            if (!pool.Exists)
            {
                return;
            }

            var pools = await poolCache.FindPoolsForTokenAsync(usdc);

            Assert.NotNull(pools);
            Assert.True(pools.Count > 0, "Should find cached pool for USDC");
            Assert.All(pools, p =>
            {
                Assert.True(
                    p.Currency0.Equals(usdc, StringComparison.OrdinalIgnoreCase) ||
                    p.Currency1.Equals(usdc, StringComparison.OrdinalIgnoreCase),
                    $"Pool {p.PoolId} doesn't contain USDC");
            });

            var cachedPools = await poolCache.GetAllCachedPoolsAsync();
            Assert.True(cachedPools.Count >= pools.Count);
        }

        [Fact]
        public async Task TestPoolCacheHandlesUnorderedTokens()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(web3, UniswapAddresses.MainnetStateViewV4);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            var poolForward = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 10);
            var poolReverse = await poolCache.GetOrFetchPoolAsync(usdc, eth, 500, 10);

            Assert.True(poolForward.Exists, "Expected ETH/USDC pool to exist");
            Assert.True(poolReverse.Exists, "Expected USDC/ETH query to resolve same pool");
            Assert.Equal(poolForward.PoolId, poolReverse.PoolId);
            Assert.Equal(poolForward.Currency0, poolReverse.Currency0);
            Assert.Equal(poolForward.Currency1, poolReverse.Currency1);
        }

        [Fact]
        public async Task TestPoolCacheReturnsMissingPoolAsNonExisting()
        {
            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var poolCache = new V4PoolCache(web3, UniswapAddresses.MainnetStateViewV4);

            var eth = AddressUtil.ZERO_ADDRESS;
            var usdc = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48";

            var missingPool = await poolCache.GetOrFetchPoolAsync(eth, usdc, 500, 5);

            Assert.False(missingPool.Exists);
            Assert.Equal(AddressUtil.Current.ConvertToChecksumAddress(eth), missingPool.Currency0);
            Assert.Equal(AddressUtil.Current.ConvertToChecksumAddress(usdc), missingPool.Currency1);
        }

    }
}
