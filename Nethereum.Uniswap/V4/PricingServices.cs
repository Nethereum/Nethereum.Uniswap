using System;
using Nethereum.Uniswap.V4.Mappers;
using Nethereum.Uniswap.V4.V4Quoter;
using Nethereum.Web3;

namespace Nethereum.Uniswap.V4
{
    /// <summary>
    /// Lightweight container for pricing and quoting services.
    /// </summary>
    public class PricingServices
    {
        public PricingServices(
            IWeb3 web3,
            UniswapV4Addresses addresses,
            IV4PoolCacheRepository poolCacheRepository,
            V4QuoterService quoter = null)
        {
            if (web3 == null) throw new ArgumentNullException(nameof(web3));
            if (addresses == null) throw new ArgumentNullException(nameof(addresses));

            var poolCache = new V4PoolCache(web3, addresses.StateView, addresses.PoolManager, poolCacheRepository);

            if (string.IsNullOrWhiteSpace(addresses.Quoter))
            {
                throw new ArgumentException("Quoter address is required for pricing services", nameof(addresses));
            }

            Quoter = quoter ?? new V4QuoterService(web3, addresses.Quoter);
            PathFinder = new V4BestPathFinder(web3, addresses.Quoter, poolCache);
            PathKeyMapper = PathKeyMapper.Current;
        }

        public V4QuoterService Quoter { get; }
        public V4BestPathFinder PathFinder { get; }
        public PoolKeyMapper PoolKeyMapper { get; }
        public PathKeyMapper PathKeyMapper { get; }
    }
}
