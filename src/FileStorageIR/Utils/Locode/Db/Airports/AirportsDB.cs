using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Neo.FileStorage.InnerRing.Utils.Locode.Column;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Db
{
    public class AirportsDB
    {
        public string AirportsPath { get; init; }
        public string CountriesPath { get; init; }
        private readonly Once airportsOnce = new();
        private readonly Once countriesOnce = new();
        private readonly Dictionary<string, string> mCountries = new();
        private readonly Dictionary<string, List<Record>> mAirports = new();

        private class Record
        {
            public string City;
            public string Country;
            public string Iata;
            public double Lat;
            public double Lng;
        }

        public class Airport
        {
            [Index(0)]
            public int ID { get; set; }

            [Index(1)]
            public string Name { get; set; }

            [Index(2)]
            public string City { get; set; }

            [Index(3)]
            public string Country { get; set; }

            [Index(4)]
            public string IATA { get; set; }

            [Index(5)]
            public string ICAO { get; set; }

            [Index(6)]
            public double Latitude { get; set; }

            [Index(7)]
            public double Longitude { get; set; }

            [Index(8)]
            public int Altitude { get; set; }

            [Index(9)]
            public string Timezone { get; set; }

            [Index(10)]
            public string DST { get; set; }

            [Index(11)]
            public string TzDatabaseTimeZone { get; set; }

            [Index(12)]
            public string Type { get; set; }

            [Index(13)]
            public string Source { get; set; }

        }

        public class Country
        {
            [Index(0)]
            public string Name { get; set; }

            [Index(1)]
            public string ISOCode { get; set; }

            [Index(2)]
            public string DafifCode { get; set; }
        }

        public AirportRecord Get(LocodeRecord locodeRecord)
        {
            InitAirports();
            List<Record> records = mAirports[locodeRecord.LOCODE.CountryCode()];
            foreach (var record in records)
            {
                if (locodeRecord.LOCODE.LocationCode() != record.Iata && locodeRecord.NameWoDiacritics != record.City) continue;
                return new()
                {
                    CountryName = record.Country,
                    Point = new()
                    {
                        Latitude = record.Lat,
                        Longitude = record.Lng
                    }
                };
            }
            throw new KeyNotFoundException("airport not found");
        }


        public string CountryName(CountryCode code)
        {
            InitCountries();
            foreach (var country in mCountries)
            {
                if (country.Value == code.ToString())
                {
                    return country.Key;
                }
            }
            throw new KeyNotFoundException("country not found");
        }

        public void InitAirports()
        {
            airportsOnce.Do(() =>
            {
                InitCountries();
                ScanRecords<Airport>(AirportsPath, airport =>
                {
                    if (mCountries.TryGetValue(airport.Country, out var countryCode))
                    {
                        Record record = new()
                        {
                            City = airport.City,
                            Country = airport.Country,
                            Iata = airport.IATA,
                            Lat = airport.Latitude,
                            Lng = airport.Longitude,
                        };
                        if (mAirports.TryGetValue(countryCode, out var records))
                            records.Add(record);
                        else
                            mAirports[countryCode] = new List<Record>() { record };
                    }
                });
            });
        }
        public void InitCountries()
        {
            countriesOnce.Do(() =>
            {
                ScanRecords<Country>(CountriesPath, country =>
                {
                    mCountries[country.Name] = country.ISOCode;
                });
            });
        }

        public void ScanRecords<T>(string path, Action<T> handler)
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<T>();
            foreach (var record in records)
                handler(record);
        }
    }
}
