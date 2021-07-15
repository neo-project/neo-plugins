using System;
using Akka.Util.Internal;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public class LOCODE
    {
        private string[] values = new string[2];

        public LOCODE(string[] values)
        {
            this.values = values;
        }

        public string CountryCode() { return values[0]; }
        public string LocationCode() { return values[1]; }

        public static LOCODE FromString(string s)
        {
            string locationSeparator = " ";
            string[] words = s.Split(locationSeparator);
            if (words.Length != 1 && words.Length != 2) throw new FormatException("invalid string format in UN/Locode");
            return new LOCODE(words);
        }

        public override string ToString()
        {
            return values.Join(" ");
        }
    }
}
