﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;


namespace Nethereum.Uniswap.UniversalRouter.Commands
{
    public class V3SwapExactOutCommand: UniversalRouterCommand
    {
        public override byte CommandType { get; set; } = (byte)UniversalRouterCommandType.V3_SWAP_EXACT_OUT;

        [Parameter("address", "recipient", 1)]
        public string Recipient { get; set; }
        [Parameter("uint256", "amountOut", 2)]
        public BigInteger AmountOut { get; set; }
        [Parameter("uint256", "amountInMaximum", 3)]
        public BigInteger AmountInMaximum { get; set; }
        [Parameter("bytes", "path", 4)]
        public byte[] Path { get; set; }
        [Parameter("bool", "fundsFromPermit2OrUniversalRouter", 5)]
        public bool FundsFromPermit2OrUniversalRouter { get; set; }

       
    }


}
