using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Configuration.Attributes;
using Neo.FileStorage.InnerRing.Utils.Locode.Column;

namespace Neo.FileStorage.InnerRing.Utils.Locode
{
    public class CSVTable
    {
        private readonly string[] paths;
        private readonly string subDivPath;
        private readonly Dictionary<(string, string), string> mSubDiv = new();
        private readonly Once subDivOnce = new();

        public class SubdivisionCode
        {
            [Index(0)]
            public string Country { get; set; }

            [Index(1)]
            public string Subdivision { get; set; }

            [Index(2)]
            public string Name { get; set; }

            [Index(3)]
            public string Description { get; set; }
        }

        public class UNLOCODERecord
        {
            [Index(0)]
            public string Ch { get; set; }

            [Index(1)]
            public string CountryCode { get; set; }

            [Index(2)]
            public string LocationCode { get; set; }

            [Index(3)]
            public string Name { get; set; }

            [Index(4)]
            public string NameWoDiacritics { get; set; }

            [Index(5)]
            public string SubDiv { get; set; }

            [Index(6)]
            public string Function { get; set; }

            [Index(7)]
            public string Status { get; set; }

            [Index(8)]
            public string Date { get; set; }

            [Index(9)]
            public string IATA { get; set; }

            [Index(10)]
            public string Coordinates { get; set; }

            [Index(11)]
            public string Remarks { get; set; }
        }

        public CSVTable(string[] paths, string subDivPath)
        {
            if (paths is null) throw new ArgumentNullException(nameof(paths));
            this.paths = paths;
            this.subDivPath = subDivPath;
        }

        public void IterateAll(Action<LocodeRecord> f)
        {
            ScanRecords<UNLOCODERecord>(paths, ur =>
            {
                LocodeRecord lr = new()
                {
                    Ch = ur.Ch,
                    LOCODE = new(new string[] { ur.CountryCode, ur.LocationCode }),
                    Name = ur.Name,
                    NameWoDiacritics = ur.NameWoDiacritics,
                    SubDiv = ur.SubDiv,
                    Function = ur.Function,
                    Status = ur.Status,
                    Date = ur.Date,
                    IATA = ur.IATA,
                    Coordinates = ur.Coordinates,
                    Remarks = ur.Remarks,
                };
                f(lr);
            });
        }

        public string SubDivName(CountryCode countryCode, string code)
        {
            InitSubDiv();
            if (!mSubDiv.TryGetValue((countryCode.ToString(), code), out string rec)) throw new KeyNotFoundException("subdivision not found");
            return rec;
        }

        public void InitSubDiv()
        {
            subDivOnce.Do(() =>
            {
                ScanRecords<SubdivisionCode>(new string[] { subDivPath }, s =>
                {
                    mSubDiv[(s.Country, s.Subdivision)] = s.Name;
                });
            });
        }

        public void ScanRecords<T>(string[] paths, Action<T> handler)
        {
            foreach (string path in paths)
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
}
