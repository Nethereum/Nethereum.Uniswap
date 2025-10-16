using Nethereum.Uniswap.V4;

namespace Nethereum.Uniswap.V4
{
    /// <summary>
    /// Lightweight container exposing math helpers for Uniswap V4.
    /// </summary>
    public class MathServices
    {
        public V4LiquidityMath Liquidity => V4LiquidityMath.Current;
        public V4PriceCalculator Price => V4PriceCalculator.Current;
        public V4TickMath Tick => V4TickMath.Current;
        public V4FeeCalculator Fee => V4FeeCalculator.Current;
        public V4SlippageCalculator Slippage => V4SlippageCalculator.Current;
        public V4PriceImpactCalculator PriceImpact => V4PriceImpactCalculator.Current;
        public V4PositionInfoHelper PositionInfo => V4PositionInfoHelper.Current;
        public V4PositionInfoDecoder PositionInfoDecoder => V4PositionInfoDecoder.Current;
    }
}
