namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public class LongitudeCode : CordinateCode
    {
        public static LongitudeCode LongitudeFromString(string s)
        {
            return (LongitudeCode)CoordinateFromString(s, LngDegDigits, LongitudeSymbols);
        }

        public bool East()
        {
            return Hemisphere()[0] == 'E';
        }
    };
}
