namespace Neo.FileStorage.InnerRing.Utils.Locode.Db
{
    public enum Continent : byte
    {
        ContinentUnknown = 0x00,
        ContinentEurope,
        ContinentAfrica,
        ContinentNorthAmerica,
        ContinentSouthAmerica,
        ContinentAsia,
        ContinentAntarctica,
        ContinentOceania
    }

    public static class Helper
    {
        public static string String(this Continent continent)
        {
            switch (continent)
            {
                case Continent.ContinentUnknown:
                    return "Unknown";
                case Continent.ContinentEurope:
                    return "Europe";
                case Continent.ContinentAfrica:
                    return "Africa";
                case Continent.ContinentNorthAmerica:
                    return "North America";
                case Continent.ContinentSouthAmerica:
                    return "South America";
                case Continent.ContinentAsia:
                    return "Asia";
                case Continent.ContinentAntarctica:
                    return "Antarctica";
                case Continent.ContinentOceania:
                    return "Oceania";
                default:
                    return "Unknown";

            }
        }

        public static Continent ContinentFromString(this string s)
        {
            switch (s)
            {
                case "Europe":
                    return Continent.ContinentEurope;
                case "Africa":
                    return Continent.ContinentAfrica;
                case "North America":
                    return Continent.ContinentNorthAmerica;
                case "South America":
                    return Continent.ContinentSouthAmerica;
                case "Asia":
                    return Continent.ContinentAsia;
                case "Antarctica":
                    return Continent.ContinentAntarctica;
                case "Australia":
                case "Oceania":
                    return Continent.ContinentOceania;
                default:
                    return Continent.ContinentUnknown;
            }
        }
    }
}
