using System;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Column
{
    public class Coordinates
    {
        public LatitudeCode Lat;
        public LongitudeCode Lng;

        public static Coordinates CoordinatesFromString(string s)
        {
            if (s == "") return null;
            var strs = s.Split(" ");
            if (strs.Length != 2) throw new Exception("invalid string format in UN/Locode");
            var lat = LatitudeCode.LatitudeFromString(strs[0]);
            var lng = LongitudeCode.LongitudeFromString(strs[1]);
            return new Coordinates() { Lat = lat, Lng = lng };
        }
    };
}
