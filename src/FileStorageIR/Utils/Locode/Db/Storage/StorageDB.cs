using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Neo.FileStorage.Database;
using Neo.FileStorage.Database.LevelDB;
using Neo.FileStorage.InnerRing.Utils.Locode.Column;
using Neo.IO;

namespace Neo.FileStorage.InnerRing.Utils.Locode.Db
{
    public class StorageDB : IDisposable
    {
        private const byte PreLocode = 0x00;
        private readonly IDB _db;

        public StorageDB(string path)
        {
            _db = new DB(path);
        }

        public void Dispose()
        {
            _db?.Dispose();
        }

        public (Key, Record) Get(LOCODE lc)
        {
            Key key = new(lc);
            Record record = _db.Get(Key(PreLocode, key))?.AsSerializable<Record>();
            return (key, record);
        }

        public void Put(LOCODE lc, Record record)
        {
            _db.Put(Key(PreLocode, new Key(lc)), record.ToArray());
        }

        public void Put(Key key, Record record)
        {
            _db.Put(Key(PreLocode, key), record.ToArray());
        }

        private byte[] Key(byte prefix, ISerializable key)
        {
            byte[] buffer = new byte[key.Size + 1];
            using (MemoryStream ms = new(buffer, true))
            using (BinaryWriter writer = new(ms))
            {
                writer.Write(prefix);
                key.Serialize(writer);
            }
            return buffer;
        }

        public void FillDatabase(CSVTable table, AirportsDB airports, ContinentDB continents)
        {
            table.IterateAll(tableRecord =>
            {
                if (tableRecord.LOCODE.LocationCode() == "")
                    return;
                Key dbKey = new(tableRecord.LOCODE);
                Record dbRecord = new(tableRecord);
                string countryName = "";
                if (dbRecord.Point is null)
                {
                    AirportRecord airportRecord = null;
                    try
                    {
                        airportRecord = airports.Get(tableRecord);
                    }
                    catch (KeyNotFoundException)
                    {
                        return;
                    }
                    countryName = airportRecord.CountryName;
                    dbRecord.Point = airportRecord.Point;
                }
                if (countryName == "")
                {
                    try
                    {
                        countryName = airports.CountryName(dbKey.CountryCode);
                    }
                    catch (KeyNotFoundException)
                    {
                        return;
                    }
                }
                dbRecord.CountryName = countryName;
                if (dbRecord.SubDivCode != "")
                {
                    string subDevName = "";
                    try
                    {
                        subDevName = table.SubDivName(dbKey.CountryCode, dbRecord.SubDivCode);
                    }
                    catch (KeyNotFoundException)
                    {
                        return;
                    }
                    dbRecord.SubDivName = subDevName;
                }
                var continent = continents.PointContinent(dbRecord.Point);
                if (continent == Continent.ContinentUnknown) return;
                dbRecord.Continent = continent;
                Put(dbKey, dbRecord);
            });
        }
    }
}
