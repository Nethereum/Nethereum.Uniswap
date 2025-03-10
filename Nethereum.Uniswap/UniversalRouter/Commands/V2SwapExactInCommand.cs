﻿using Nethereum.ABI.FunctionEncoding.Attributes;
using System.Numerics;


namespace Nethereum.Uniswap.UniversalRouter.Commands
{
    public class V2SwapExactInCommand : UniversalRouterCommand
    {
        public override byte CommandType { get; set; } = (byte)UniversalRouterCommandType.V2_SWAP_EXACT_IN;

        [Parameter("address", "recipient", 1)]
        public string Recipient { get; set; }

        [Parameter("uint256", "amountIn", 2)]
        public BigInteger AmountIn { get; set; }

        [Parameter("uint256", "amountOutMinimum", 3)]
        public BigInteger AmountOutMinimum { get; set; }

        [Parameter("address[]", "path", 4)]
        public string[] Path { get; set; }

        [Parameter("bool", "fundsFromPermit2OrUniversalRouter", 5)]
        public bool FundsFromPermit2OrUniversalRouter { get; set; }

       
    }
    
}
