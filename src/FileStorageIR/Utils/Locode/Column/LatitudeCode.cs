namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public class LatitudeCode : CordinateCode
    {
        public static LatitudeCode LatitudeFromString(string s)
        {
            return (LatitudeCode)CoordinateFromString(s, LatDegDigits, LatitudeSymbols);
        }

        public bool North()
        {
            return Hemisphere()[0] == 'N';
        }
    };
}
