using System;
using System.IO;
using Neo.FileStorage.InnerRing.Utils.Locode.Column;
using Neo.IO;
using static Neo.Utility;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Db
{
    public class Point : ISerializable
    {
        public double Latitude;
        public double Longitude;

        public int Size => 2 * sizeof(double);

        public void Deserialize(BinaryReader reader)
        {
            Latitude = reader.ReadDouble();
            Longitude = reader.ReadDouble();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(Latitude);
            writer.Write(Longitude);
            writer.Flush();
        }

        public static Point PointFromCoordinates(Coordinates crd)
        {
            if (crd is null) return null;
            var cLat = crd.Lat;
            var cLatDeg = cLat.Degrees();
            var cLatMnt = cLat.Minutes();
            var lat = Double.Parse(StrictUTF8.GetString(cLatDeg) + "." + StrictUTF8.GetString(cLatMnt));
            if (!cLat.North()) lat = -lat;
            var cLng = crd.Lng;
            var cLngDeg = cLng.Degrees();
            var cLngMnt = cLng.Minutes();
            var lng = Double.Parse(StrictUTF8.GetString(cLngDeg) + "." + StrictUTF8.GetString(cLngMnt));
            if (!cLng.East()) lng = -lng;
            return new Point() { Latitude = lat, Longitude = lng };
        }

        public override string ToString()
        {
            return Latitude + ", " + Longitude;
        }
    }
}
