using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;

namespace Nethereum.Uniswap.UniversalRouter.V4Actions
{
    public class BurnPosition : V4ActionRouterCommand
    {
        public override byte CommandType { get; set; } = (byte)UniversalRouterV4ActionTypes.BURN_POSITION;

        [Parameter("uint256", "positionId", 1)]
        public BigInteger PositionId { get; set; }

        [Parameter("uint128", "amount0", 2)]
        public BigInteger Amount0 { get; set; }

        [Parameter("uint128", "amount1", 3)]
        public BigInteger Amount1 { get; set; }

        [Parameter("bytes", "hookData", 4)]
        public byte[] HookData { get; set; }
    }

}

