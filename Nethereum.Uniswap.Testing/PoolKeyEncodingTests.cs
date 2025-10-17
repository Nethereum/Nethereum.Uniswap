using Nethereum.ABI;
using Nethereum.Uniswap.V4.Pools;

using Nethereum.Uniswap.V4.Positions.PositionManager.ContractDefinition;
using Nethereum.Util;
using Xunit;

namespace Nethereum.Uniswap.Testing
{
    public class PoolKeyEncodingTests
    {
        [Fact]
        public void EncodePoolKeyMatchesAbiEncoding()
        {
            var poolKey = new PoolKey
            {
                Currency0 = AddressUtil.ZERO_ADDRESS,
                Currency1 = "0xA0b86991c6218b36c1d19D4a2e9Eb0cE3606eB48",
                Fee = 500,
                TickSpacing = 10,
                Hooks = "0x24F7c9ea6B5be5227caAeB61366b56052386eae4"
            };

            var abiEncode = new ABIEncode();
            var expectedEncoding = abiEncode.GetABIEncoded(
                new ABIValue("address", poolKey.Currency0),
                new ABIValue("address", poolKey.Currency1),
                new ABIValue("uint24", poolKey.Fee),
                new ABIValue("int24", poolKey.TickSpacing),
                new ABIValue("address", poolKey.Hooks));

            var actualEncoding = poolKey.EncodePoolKey();

            Assert.Equal(expectedEncoding.Length, actualEncoding.Length);
            Assert.Equal(expectedEncoding, actualEncoding);

            var expectedPoolId = Sha3Keccack.Current.CalculateHash(expectedEncoding);
            var actualPoolId = Sha3Keccack.Current.CalculateHash(actualEncoding);

            Assert.Equal(expectedPoolId, actualPoolId);
        }
    }
}
