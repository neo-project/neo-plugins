using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Neo.FileStorage.Database;
using Neo.FileStorage.Database.LevelDB;
using Neo.FileStorage.InnerRing.Utils.Locode;
using Neo.FileStorage.InnerRing.Utils.Locode.Column;
using Neo.FileStorage.InnerRing.Utils.Locode.Db;
using Neo.FileStorage.Utils;
using Neo.IO;

namespace Neo.FileStorage.InnerRing.Tests.Util.Locode
{
    [TestClass]
    public class UT_Locode
    {
        // FillDatabase will take a long time to generate files so we commented these out
        // [TestMethod]
        // public void TestFillDatabase()
        // {
        //     string resourcePath = "./Resources/";
        //     string[] tableInPaths = new string[]
        //     {
        //         resourcePath + "2020-2 UNLOCODE CodeListPart1.csv",
        //         resourcePath + "2020-2 UNLOCODE CodeListPart2.csv",
        //         resourcePath + "2020-2 UNLOCODE CodeListPart3.csv",
        //     };
        //     string tableSubDivPath = resourcePath + "2020-2 SubdivisionCodes.csv";
        //     string airportsPath = resourcePath + "airports.dat";
        //     string countriesPath = resourcePath + "countries.dat";
        //     string continentsPath = resourcePath + "continents.geojson";
        //     CSVTable locodeDB = new(tableInPaths, tableSubDivPath);
        //     AirportsDB airportsDB = new()
        //     {
        //         AirportsPath = airportsPath,
        //         CountriesPath = countriesPath
        //     };
        //     ContinentDB continentDB = new()
        //     {
        //         Path = continentsPath
        //     };
        //     string dbPath = "./Data_LOCODE";
        //     using StorageDB targetDb = new(dbPath);
        //     targetDb.FillDatabase(locodeDB, airportsDB, continentDB);
        //     Directory.Delete(dbPath, true);
        // }

        // [TestMethod]
        // public void TestResult1()
        // {
        //     Dictionary<string, string> cases = new()
        //     {
        //         { "RU LED", "Russia, Saint Petersburg (ex Leningrad), Sankt-Peterburg, 59.53, 30.15, ContinentEurope" },
        //         { "SE STO", "Sweden, Stockholm, Stockholms l�n, 59.2, 18.03, ContinentEurope" },
        //         { "RU MSK", "Russia, Mishkino, , 55.2, 63.53, ContinentAsia" },
        //         { "FI HEL", "Finland, Helsinki (Helsingfors), Uusimaa, 60.317199707031, 24.963300704956, ContinentEurope" },
        //     };
        //     string dbPath = "./Data_LOCODE";
        //     using StorageDB targetDb = new(dbPath);
        //     foreach (var (locode, expected) in cases)
        //     {
        //         var r = targetDb.Get(LOCODE.FromString(locode));
        //         Assert.IsNotNull(r.Item2);
        //         Assert.AreEqual(expected, r.Item2.ToString());
        //     }
        // }

        // [TestMethod]
        // public void TestResult2()
        // {
        //     string dbPath = "./Data_LOCODE";
        //     using StorageDB targetDb = new(dbPath);
        //     var r = targetDb.Get(LOCODE.FromString("AU ADL"));
        //     Assert.IsNotNull(r.Item2);
        //     Assert.AreEqual("", r.Item2.ToString());
        // }

        [TestMethod]
        public void TestReadCountries()
        {
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };
            using (var reader = new StreamReader("./Resources/countries.dat"))
            using (var csv = new CsvReader(reader, config))
            {
                var records = csv.GetRecords<AirportsDB.Country>();
                Assert.AreEqual(records.Count(), 261);
            }
        }

        [TestMethod]
        public void TestReadEmptyRecord()
        {
            string path = "test.csv";
            string content = ",\"AD\",,\".ANDORRA\",,,,,,,,";
            File.WriteAllText(path, content);
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<CSVTable.UNLOCODERecord>();
            var record = records.First();
            Assert.AreEqual("AD", record.CountryCode);
            File.Delete(path);
        }

        [TestMethod]
        public void TestReadUNLOCODE()
        {
            string path = "./Resources/2020-2 UNLOCODE CodeListPart1.csv";
            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                HasHeaderRecord = false,
            };

            using var reader = new StreamReader(path);
            using var csv = new CsvReader(reader, config);

            var records = csv.GetRecords<CSVTable.UNLOCODERecord>();
            Assert.AreEqual(50990, records.Count());
        }

        [TestMethod]
        public void TestContinents()
        {
            List<(Point, Continent)> cases = new()
            {
                { (new() { Latitude = 48.25, Longitude = 15.45 }, Continent.ContinentEurope) },
                { (new() { Latitude = -34.55, Longitude = 138.35 }, Continent.ContinentOceania) },
            };
            string continentsPath = "./Resources/continents.geojson";
            ContinentDB continentDB = new()
            {
                Path = continentsPath
            };
            foreach (var (p, expected) in cases)
            {
                Assert.AreEqual(expected, continentDB.PointContinent(p));
            }
        }

        [TestMethod]
        public void TestAirport()
        {
            string resourcePath = "./Resources/";
            string airportsPath = resourcePath + "airports.dat";
            string countriesPath = resourcePath + "countries.dat";
            AirportsDB airportsDB = new()
            {
                AirportsPath = airportsPath,
                CountriesPath = countriesPath
            };
            var r = airportsDB.Get(new()
            {
                LOCODE = LOCODE.FromString("FI HEL"),
                NameWoDiacritics = "Helsingfors (Helsinki)",
            });
            Assert.IsNotNull(r);
            Assert.AreEqual("60.317199707031, 24.963300704956", r.Point.ToString());
        }
    }
}
