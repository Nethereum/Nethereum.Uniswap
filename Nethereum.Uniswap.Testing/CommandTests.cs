using Nethereum.Hex.HexConvertors.Extensions;
using Nethereum.Uniswap.Core.Permit2.ContractDefinition;
using Nethereum.Uniswap.UniversalRouter;
using Nethereum.Uniswap.UniversalRouter.Commands;
using System;
using System.Diagnostics;
using System.Linq;
using Xunit;

namespace Nethereum.Uniswap.Testing
{
    public class CommandTests
    {
        [Fact]
        public void ShouldCreateValidCommandType()
        {
            var permitCommand = new Permit2PermitCommand();
            permitCommand.AllowRevert = true;
            var fullCommandType = permitCommand.GetFullCommandType();
            
            var wrapEthCommand = new WrapEthCommand();
            var fullCommandTypeWrapEth = wrapEthCommand.GetFullCommandType();

            var planner = new UniversalRouterBuilder();
            planner.AddCommand(permitCommand);
            planner.AddCommand(wrapEthCommand);

            var fullCommands = planner.GetCommands();
            var binary = string.Join(" ", fullCommands.Select(x => Convert.ToString(x, 2).PadLeft(8, '0')));
            Debug.WriteLine(binary);

        }


        [Fact]
        public void ShouldEncodeDecodeCommands()
        {
            var universalRouter = UniswapAddresses.SepoliaUniversalRouterV3;
            var weth9 = "0xfff9976782d46cc05630d1f6ebab18b2324d6b14";
            var recipient = "0x12890d2cce102216644c59daE5baed380d84830c";

            var permit = new PermitSingle()
            {
                Spender = universalRouter,
                SigDeadline = 2000000000,
                Details = new PermitDetails()
                {
                    Amount = 100000,
                    Expiration = 0,
                    Nonce = 0,
                    Token = weth9
                }
            };

            var permitCommand = new Permit2PermitCommand();
            permitCommand.AllowRevert = true;
            permitCommand.Permit = permit;
            permitCommand.Signature = "0x00".HexToByteArray();


            var wrapEthCommand = new WrapEthCommand();
            wrapEthCommand.Amount = 100000;
            wrapEthCommand.Recipient = recipient;

            var planner = new UniversalRouterBuilder();
            planner.AddCommand(permitCommand);
            planner.AddCommand(wrapEthCommand);

            var commands = planner.GetCommands();
            var inputs = planner.GetInputData();

            var decoder = new UniversalRouterDecoder();
            var decodedCommands = decoder.Decode(commands, inputs);

            Assert.Equal(2, decodedCommands.Count);
            Assert.IsType<Permit2PermitCommand>(decodedCommands[0]);
            Assert.IsType<WrapEthCommand>(decodedCommands[1]);

            var decodedPermitCommand = (Permit2PermitCommand)decodedCommands[0];
            Assert.Equal(permitCommand.Permit.Details.Amount, decodedPermitCommand.Permit.Details.Amount);
            Assert.Equal(permitCommand.Permit.Details.Token.ToLower(), decodedPermitCommand.Permit.Details.Token.ToLower());
            Assert.Equal(permitCommand.Permit.Details.Expiration, decodedPermitCommand.Permit.Details.Expiration);
            Assert.Equal(permitCommand.Permit.Details.Nonce, decodedPermitCommand.Permit.Details.Nonce);
            Assert.Equal(permitCommand.Permit.SigDeadline, decodedPermitCommand.Permit.SigDeadline);
            Assert.Equal(permitCommand.Permit.Spender.ToLower(), decodedPermitCommand.Permit.Spender.ToLower());
            Assert.Equal(permitCommand.Signature, decodedPermitCommand.Signature);
            Assert.Equal(permitCommand.AllowRevert, decodedPermitCommand.AllowRevert);

            var decodedWrapEthCommand = (WrapEthCommand)decodedCommands[1];
            Assert.Equal(wrapEthCommand.Amount, decodedWrapEthCommand.Amount);
            Assert.Equal(wrapEthCommand.Recipient.ToLower(), decodedWrapEthCommand.Recipient.ToLower());
              

        }
    }
}