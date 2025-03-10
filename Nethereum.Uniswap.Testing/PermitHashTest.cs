using Nethereum.ABI.EIP712;
using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Uniswap.Core.Permit2;
using Nethereum.Uniswap.Core.Permit2.ContractDefinition;
using Nethereum.Uniswap.Permit2;
using Nethereum.Uniswap.Permit2.ContractDefinition;
using Nethereum.XUnitEthereumClients;
using System.Threading.Tasks;
using Xunit;

namespace Nethereum.Uniswap.Testing
{
    [Collection(EthereumClientIntegrationFixture.ETHEREUM_CLIENT_COLLECTION_DEFAULT)]
    public class PermitHashTest
    {
        private readonly EthereumClientIntegrationFixture _ethereumClientIntegrationFixture;
        public PermitHashTest(EthereumClientIntegrationFixture ethereumClientIntegrationFixture)
        {
            _ethereumClientIntegrationFixture = ethereumClientIntegrationFixture;
        }


        [Fact]
        public async Task ShouldGetPermitHash()
        {
            var universalRouter = "0x3fC91A3afd70395Cd496C647d5a6CC9D4B2b7FAD";
            var weth9 = "0x3fC91A3afd70395Cd496C647d5a6CC9D4B2b7FAD";

            var privateKey = EthereumClientIntegrationFixture.AccountPrivateKey;

            var web3 = _ethereumClientIntegrationFixture.GetWeb3();
            var chainId = await web3.Eth.ChainId.SendRequestAsync();

            var encoder = new Eip712TypedDataEncoder();
            var encodedType = encoder.GetEncodedType("PermitSingle", typeof(PermitSingle), typeof(PermitDetails));

            var permitService = await Permit2Service.DeployContractAndGetServiceAsync(web3, new Permit2Deployment());
            
            var permit = new PermitSingle()
            {
                Spender = universalRouter,
                SigDeadline = 2000000000,
                Details = new PermitDetails()
                {
                    Amount = 100000,
                    Expiration = 0,
                    Nonce = 0, //the nonce will be set when signing
                    Token = weth9
                }
            };

            var permitHash = encoder.HashStruct(permit, "PermitSingle", typeof(PermitSingle), typeof(PermitDetails));

           
            var permitHashContract = await permitService.HashPermitSingleQueryAsync(permit);
            var permithashHex = permitHash.ToHex();
            var permithashContractHex = permitHashContract.ToHex();

            Assert.Equal(permithashHex, permithashContractHex);

             var signedPermit = await permitService.GetSinglePermitWithSignatureAsync(permit, new Signer.EthECKey(privateKey));
            var receipt = await permitService.PermitRequestAndWaitForReceiptAsync(EthereumClientIntegrationFixture.AccountAddress, signedPermit.PermitRequest, signedPermit.GetSignatureBytes());

        }
    }
}
