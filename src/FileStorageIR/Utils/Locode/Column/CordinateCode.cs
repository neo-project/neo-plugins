using System;
using System.Linq;
using System.Text.RegularExpressions;
using static Neo.Utility;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public abstract class CordinateCode
    {
        public readonly static char[] LatitudeSymbols = new char[] { 'N', 'S' };
        public readonly static char[] LongitudeSymbols = new char[] { 'W', 'E' };
        public const int MinutesDigits = 2;
        public const int HemisphereSymbols = 1;
        public const int LatDegDigits = 2;
        public const int LngDegDigits = 3;
        private int degDigits;
        private byte[] value;

        public static CordinateCode CoordinateFromString(string s, int degDigits, char[] hemisphereAlphabet)
        {
            if (s.Length != degDigits + MinutesDigits + HemisphereSymbols) throw new FormatException("invalid string format in UN/Locode");
            string pattern = @"^[0-9]+$";
            if (!Regex.IsMatch(s.Substring(0, degDigits + MinutesDigits), pattern)) throw new FormatException("invalid string format in UN/Locode");
            if (!hemisphereAlphabet.Contains(s[^1])) throw new FormatException("invalid string format in UN/Locode");
            if (hemisphereAlphabet == LatitudeSymbols)
                return new LatitudeCode() { degDigits = degDigits, value = StrictUTF8.GetBytes(s) };
            else if (hemisphereAlphabet == LongitudeSymbols)
                return new LongitudeCode() { degDigits = degDigits, value = StrictUTF8.GetBytes(s) };
            throw new InvalidOperationException();
        }

        public byte[] Hemisphere()
        {
            return value.Skip(degDigits + MinutesDigits).ToArray();
        }

        public byte[] Degrees()
        {
            return value.Take(degDigits).ToArray();
        }

        public byte[] Minutes()
        {
            return value.Skip(degDigits).Take(MinutesDigits).ToArray();
        }
    }
}
