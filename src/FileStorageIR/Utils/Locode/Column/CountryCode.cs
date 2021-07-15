using System;
using System.Text.RegularExpressions;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public class CountryCode
    {
        public const int CountryCodeLen = 2;
        private char[] values = new char[CountryCodeLen];

        public CountryCode(char[] values)
        {
            this.values = values;
        }

        public char[] Symbols()
        {
            return values;
        }

        public static CountryCode CountryCodeFromString(string s)
        {
            string pattern = @"^[A-Z]+$";
            if (s.Length != CountryCodeLen) throw new FormatException("invalid string format in UN/Locode");
            if (!Regex.IsMatch(s, pattern)) throw new FormatException("invalid string format in UN/Locode");
            return new CountryCode(s.ToCharArray());
        }

        public override string ToString()
        {
            return new string(values);
        }
    }
}
