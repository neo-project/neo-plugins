using System;
using System.Numerics;

namespace Neo.FileStorage.InnerRing.Utils
{
    public class ConvertUtil
    {
        public uint BasePrecision;
        public uint TargetPrecision;
        public BigInteger Factor;

        private BigInteger Convert(BigInteger n, BigInteger factor, bool decreasePrecision)
        {
            if (decreasePrecision)
                return BigInteger.Divide(n, factor);
            return BigInteger.Multiply(n, factor);
        }

        public BigInteger ToBasePrecision(BigInteger n)
        {
            return Convert(n, Factor, BasePrecision < TargetPrecision);
        }

        public BigInteger ToTargetPrecision(BigInteger n)
        {
            return Convert(n, Factor, BasePrecision > TargetPrecision);
        }

        public BigInteger Convert(uint fromPrecision, uint toPrecision, BigInteger n)
        {
            bool decreasePrecision = false;
            var exp = (int)toPrecision - (int)fromPrecision;
            if (exp < 0)
            {
                decreasePrecision = true;
                exp = -exp;
            }
            Factor = new BigInteger(Math.Pow(10, exp));
            return Convert(n, Factor, decreasePrecision);
        }
    }

    public class Fixed8ConverterUtil : ConvertUtil
    {
        private const uint Fixed8Precision = 8;

        public Fixed8ConverterUtil()
        {
        }

        public Fixed8ConverterUtil(uint precision)
        {
            SetBalancePrecision(precision);
        }

        public long ToFixed8(long n)
        {
            return (long)ToBasePrecision(new BigInteger(n));
        }

        public long ToBalancePrecision(long n)
        {
            return (long)ToTargetPrecision(new BigInteger(n));
        }

        public void SetBalancePrecision(uint precision)
        {
            var exp = (int)precision - Fixed8Precision;
            if (exp < 0)
                exp = -exp;
            BasePrecision = Fixed8Precision;
            TargetPrecision = precision;
            Factor = new BigInteger(Math.Pow(10, exp));
        }
    }
}
