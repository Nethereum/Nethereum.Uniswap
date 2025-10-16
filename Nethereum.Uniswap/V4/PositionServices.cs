using System;
using Nethereum.Uniswap.V4.PositionManager;
using Nethereum.Uniswap.V4.StateView;
using Nethereum.Web3;

namespace Nethereum.Uniswap.V4
{
    /// <summary>
    /// Lightweight container for position-related services.
    /// </summary>
    public class PositionServices
    {
        public PositionServices(
            IWeb3 web3,
            UniswapV4Addresses addresses)
        {
            if (web3 == null) throw new ArgumentNullException(nameof(web3));
            if (addresses == null) throw new ArgumentNullException(nameof(addresses));
            if (string.IsNullOrWhiteSpace(addresses.PositionManager))
            {
                throw new ArgumentException("PositionManager address is required for position services", nameof(addresses));
            }

            if (string.IsNullOrWhiteSpace(addresses.StateView))
            {
                throw new ArgumentException("StateView address is required for position services", nameof(addresses));
            }

            Manager = new PositionManagerService(web3, addresses.PositionManager);
            StateView = new StateViewService(web3, addresses.StateView);
            ValueCalculator = new V4PositionValueCalculator(web3, addresses.PositionManager, addresses.StateView);
        }

        public PositionManagerService Manager { get; }
        public StateViewService StateView { get; }
        public V4PositionValueCalculator ValueCalculator { get; }
        public V4PositionManagerActionsBuilder CreatePositionManagerActionsBuilder() => new V4PositionManagerActionsBuilder();
    }
}
