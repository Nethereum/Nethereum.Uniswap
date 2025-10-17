using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Nethereum.Uniswap.V4.Pools
{
    public class InMemoryPoolCacheRepository : IPoolCacheRepository
    {
        private readonly ConcurrentDictionary<string, PoolCacheEntry> _cache = new ConcurrentDictionary<string, PoolCacheEntry>(StringComparer.OrdinalIgnoreCase);

        public Task<PoolCacheEntry> GetPoolAsync(string poolId)
        {
            _cache.TryGetValue(poolId, out var entry);
            return Task.FromResult(entry);
        }

        public Task SavePoolAsync(PoolCacheEntry entry)
        {
            _cache[entry.PoolId] = entry;
            return Task.CompletedTask;
        }

        public Task<List<PoolCacheEntry>> GetAllPoolsAsync()
        {
            return Task.FromResult(_cache.Values.ToList());
        }

        public Task ClearAsync()
        {
            _cache.Clear();
            return Task.CompletedTask;
        }
    }
}





