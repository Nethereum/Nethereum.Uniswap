using Nethereum.Uniswap.UniversalRouter.V4Actions;
using Nethereum.Uniswap.V4.V4Quoter.ContractDefinition;
using System.Collections.Generic;
using System.Linq;

namespace Nethereum.Uniswap.V4.Mappers
{
    public class PoolKeyMapper
    {
        public static PoolKeyMapper Current { get; } = new PoolKeyMapper();

        public UniversalRouter.V4Actions.PoolKey MapToV4Action(V4Quoter.ContractDefinition.PoolKey poolKey)
        {
            return new UniversalRouter.V4Actions.PoolKey
            {
                Currency0 = poolKey.Currency0,
                Currency1 = poolKey.Currency1,
                Fee = poolKey.Fee,
                Hooks = poolKey.Hooks,
                TickSpacing = poolKey.TickSpacing
            };
        }

        public V4Quoter.ContractDefinition.PoolKey MapToV4Quoter(UniversalRouter.V4Actions.PoolKey poolKey)
        {
            return new V4Quoter.ContractDefinition.PoolKey
            {
                Currency0 = poolKey.Currency0,
                Currency1 = poolKey.Currency1,
                Fee = poolKey.Fee,
                Hooks = poolKey.Hooks,
                TickSpacing = poolKey.TickSpacing
            };
        }

        public List<UniversalRouter.V4Actions.PoolKey> MapToV4Action(List<V4Quoter.ContractDefinition.PoolKey> poolKeys)
        {
            return poolKeys.Select(MapToV4Action).ToList();
        }

        public List<V4Quoter.ContractDefinition.PoolKey> MapToV4Quoters(List<UniversalRouter.V4Actions.PoolKey> poolKeys)
        {
            return poolKeys.Select(MapToV4Quoter).ToList();
        }
    }
}
