using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public class LocationCode
    {
        public const int LocationCodeLen = 3;
        private char[] values = new char[LocationCodeLen];

        public LocationCode(char[] values)
        {
            this.values = values;
        }

        public char[] Symbols()
        {
            return values;
        }

        public static LocationCode LocationCodeFromString(string s)
        {
            string pattern = @"^[A-Z0-9]+$";
            if (s.Length != LocationCodeLen) throw new Exception("invalid string format in UN/Locode");
            if (!Regex.IsMatch(s, pattern)) throw new Exception("invalid string format in UN/Locode");
            return new LocationCode(s.ToCharArray());
        }

        public override string ToString()
        {
            return new string(values);
        }
    }
}
