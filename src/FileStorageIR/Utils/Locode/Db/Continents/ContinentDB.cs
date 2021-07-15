using System.IO;
using System.Linq;
using NetTopologySuite.Features;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Db
{
    public class ContinentDB
    {
        public string Path { get; init; }
        private readonly Once once = new();
        private IFeature[] features;

        public Continent PointContinent(Point point)
        {
            once.Do(Init);
            NetTopologySuite.Geometries.Point planarPoint = new(point.Longitude, point.Latitude);
            string continent = null;
            foreach (var feature in features)
            {
                if (feature.Geometry is MultiPolygon m)
                {
                    if (m.Contains(planarPoint))
                    {
                        continent = feature.Attributes["Continent"].ToString();
                        break;
                    }
                }
                else if (feature.Geometry is Polygon p)
                {
                    if (p.Contains(planarPoint))
                    {
                        continent = feature.Attributes["Continent"].ToString();
                        break;
                    }
                }
            }
            return continent.ContinentFromString();
        }

        private void Init()
        {
            string data = File.ReadAllText(Path);
            var reader = new GeoJsonReader();
            FeatureCollection collection = reader.Read<FeatureCollection>(data);
            features = collection.ToArray();
        }
    }
}
