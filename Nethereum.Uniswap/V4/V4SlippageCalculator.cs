using System;
using System.Numerics;

namespace Nethereum.Uniswap.V4
{
    public class SlippageResult
    {
        public BigInteger OriginalAmount { get; set; }
        public BigInteger AmountWithSlippage { get; set; }
        public decimal SlippageTolerancePercentage { get; set; }
        public BigInteger SlippageAmount { get; set; }
    }

    public class SlippageValidationResult
    {
        public bool IsValid { get; set; }
        public decimal ActualSlippagePercentage { get; set; }
        public decimal TolerancePercentage { get; set; }
        public string Message { get; set; }
    }

    public static class V4SlippageCalculator
    {
        public static SlippageResult CalculateMinimumAmountOut(BigInteger amountOut, decimal slippageTolerancePercentage)
        {
            if (slippageTolerancePercentage < 0 || slippageTolerancePercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(slippageTolerancePercentage), "Slippage tolerance must be between 0 and 100");
            }

            if (amountOut <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amountOut), "Amount must be positive");
            }

            var slippageFactor = 1 - (slippageTolerancePercentage / 100m);
            var minimumAmount = MultiplyByDecimal(amountOut, slippageFactor);
            var slippageAmount = amountOut - minimumAmount;

            return new SlippageResult
            {
                OriginalAmount = amountOut,
                AmountWithSlippage = minimumAmount,
                SlippageTolerancePercentage = slippageTolerancePercentage,
                SlippageAmount = slippageAmount
            };
        }

        public static SlippageResult CalculateMaximumAmountIn(BigInteger amountIn, decimal slippageTolerancePercentage)
        {
            if (slippageTolerancePercentage < 0 || slippageTolerancePercentage > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(slippageTolerancePercentage), "Slippage tolerance must be between 0 and 100");
            }

            if (amountIn <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(amountIn), "Amount must be positive");
            }

            var slippageFactor = 1 + (slippageTolerancePercentage / 100m);
            var maximumAmount = MultiplyByDecimal(amountIn, slippageFactor);
            var slippageAmount = maximumAmount - amountIn;

            return new SlippageResult
            {
                OriginalAmount = amountIn,
                AmountWithSlippage = maximumAmount,
                SlippageTolerancePercentage = slippageTolerancePercentage,
                SlippageAmount = slippageAmount
            };
        }

        public static decimal CalculateSlippagePercentage(BigInteger expectedAmount, BigInteger actualAmount)
        {
            if (expectedAmount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(expectedAmount), "Expected amount must be positive");
            }

            if (actualAmount < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(actualAmount), "Actual amount cannot be negative");
            }

            var difference = BigInteger.Abs(expectedAmount - actualAmount);
            var percentageDecimal = (decimal)difference / (decimal)expectedAmount * 100m;

            return percentageDecimal;
        }

        public static SlippageValidationResult ValidateSlippage(
            BigInteger expectedAmount,
            BigInteger actualAmount,
            decimal slippageTolerancePercentage,
            bool isAmountOut = true)
        {
            if (expectedAmount <= 0)
            {
                return new SlippageValidationResult
                {
                    IsValid = false,
                    ActualSlippagePercentage = 0,
                    TolerancePercentage = slippageTolerancePercentage,
                    Message = "Expected amount must be positive"
                };
            }

            var actualSlippage = CalculateSlippagePercentage(expectedAmount, actualAmount);

            bool isValid;
            string message;

            if (isAmountOut)
            {
                isValid = actualAmount >= expectedAmount || actualSlippage <= slippageTolerancePercentage;
                message = isValid
                    ? $"Slippage {actualSlippage:F2}% is within tolerance {slippageTolerancePercentage:F2}%"
                    : $"Slippage {actualSlippage:F2}% exceeds tolerance {slippageTolerancePercentage:F2}%";
            }
            else
            {
                isValid = actualAmount <= expectedAmount || actualSlippage <= slippageTolerancePercentage;
                message = isValid
                    ? $"Slippage {actualSlippage:F2}% is within tolerance {slippageTolerancePercentage:F2}%"
                    : $"Slippage {actualSlippage:F2}% exceeds tolerance {slippageTolerancePercentage:F2}%";
            }

            return new SlippageValidationResult
            {
                IsValid = isValid,
                ActualSlippagePercentage = actualSlippage,
                TolerancePercentage = slippageTolerancePercentage,
                Message = message
            };
        }

        public static (BigInteger minAmount0, BigInteger minAmount1) CalculateMinimumLiquidityAmounts(
            BigInteger amount0,
            BigInteger amount1,
            decimal slippageTolerancePercentage)
        {
            var result0 = CalculateMinimumAmountOut(amount0, slippageTolerancePercentage);
            var result1 = CalculateMinimumAmountOut(amount1, slippageTolerancePercentage);

            return (result0.AmountWithSlippage, result1.AmountWithSlippage);
        }

        public static BigInteger ApplySlippageTolerance(BigInteger amount, decimal slippageTolerancePercentage, bool isMinimum)
        {
            if (isMinimum)
            {
                return CalculateMinimumAmountOut(amount, slippageTolerancePercentage).AmountWithSlippage;
            }
            else
            {
                return CalculateMaximumAmountIn(amount, slippageTolerancePercentage).AmountWithSlippage;
            }
        }

        private static BigInteger MultiplyByDecimal(BigInteger value, decimal multiplier)
        {
            var scaleFactor = 1_000_000m;
            var scaledMultiplier = (BigInteger)(multiplier * scaleFactor);
            var result = (value * scaledMultiplier) / (BigInteger)scaleFactor;
            return result;
        }
    }
}
