using System.IO;
using Neo.FileStorage.InnerRing.Utils.Locode;
using Neo.FileStorage.InnerRing.Utils.Locode.Column;
using Neo.FileStorage.InnerRing.Utils.Locode.Db;
using Neo.IO;

namespace Neo.FileStorage.InnerRing.Utils
{
    public class Record : ISerializable
    {
        public string CountryName = "";
        public string LocationName = "";
        public string SubDivName = "";
        public string SubDivCode = "";
        public Point Point;
        public Continent Continent;

        public Record()
        {
        }

        public Record(LocodeRecord lr)
        {
            var crd = Coordinates.CoordinatesFromString(lr.Coordinates);
            Point = Point.PointFromCoordinates(crd);
            LocationName = lr.NameWoDiacritics;
            SubDivCode = lr.SubDiv;
        }

        public int Size => CountryName.Length + LocationName.Length + SubDivName.Length + SubDivCode.Length + Point.Size + 1;

        public void Deserialize(BinaryReader reader)
        {
            CountryName = reader.ReadString();
            LocationName = reader.ReadString();
            SubDivName = reader.ReadString();
            SubDivCode = reader.ReadString();
            Point = reader.ReadSerializable<Point>();
            Continent = (Continent)reader.ReadByte();
        }

        public void Serialize(BinaryWriter writer)
        {
            writer.Write(CountryName);
            writer.Write(LocationName);
            writer.Write(SubDivName);
            writer.Write(SubDivCode);
            writer.Write(Point);
            writer.Write((byte)Continent);
            writer.Flush();
        }

        public override string ToString()
        {
            return CountryName + ", " + LocationName + ", " + SubDivName + ", " + Point.ToString() + ", " + Continent.ToString();
        }
    }
}
