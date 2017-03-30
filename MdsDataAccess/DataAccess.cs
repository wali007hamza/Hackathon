using MdsDataAccess.DTO;
using MdsDataAccess.Extensions;
using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace MdsDataAccess
{
    public class DataAccess
    {
        private readonly Lazy<ConnectionMultiplexer> _redis =
            new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("localhost:6379"));

        public void SaveData(IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>> durationQuantiles)
        {
            var redisDb = _redis.Value.GetDatabase();

            var hashList = new List<HashEntry>();
            foreach (var kv in durationQuantiles)
            {
                hashList.Add(new HashEntry(kv.Key.Truncate(TimeSpan.FromMinutes(1)).Ticks, JsonConvert.SerializeObject(kv.Value)));
            }

            redisDb.HashSet("urn:durationQuantiles", hashList.ToArray());
        }

        public dynamic GetData(DateTime dateTime)
        {
            var redisDb = _redis.Value.GetDatabase();

            var key = dateTime.Truncate(TimeSpan.FromMinutes(1)).Ticks;
            var retrievedValue = redisDb.HashGet("urn:durationQuantiles", key);

            return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int, int>>>>>(retrievedValue);
        }

        public dynamic GetDataForTimeRange(DateTime startTime, DateTime endTime)
        {
            startTime = startTime.Truncate(TimeSpan.FromMinutes(1));
            endTime = endTime.Truncate(TimeSpan.FromMinutes(1));

            var redisDb = _redis.Value.GetDatabase();
            var dataForTimeRange = new List<IDictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int, int>>>>>();
            while (startTime < endTime)
            {
                var key = endTime.Ticks;
                var retrievedValue = redisDb.HashGet("urn:durationQuantiles", key);
                if (retrievedValue.HasValue)
                {
                    dataForTimeRange.Add(JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int, int>>>>>(retrievedValue));
                }

                endTime = endTime.AddMinutes(-1);
            }

            return dataForTimeRange;
        }

        public void SaveDatapointNames(IDictionary<string, IDictionary<string, HashSet<string>>> dataPointNames)
        {
            var redisDB = _redis.Value.GetDatabase();
            var hashList = new List<HashEntry>();
            foreach (var kv in dataPointNames)
            {
                hashList.Add(new HashEntry(kv.Key, JsonConvert.SerializeObject(kv.Value)));
            }

            redisDB.HashSet("urn:datapointNames", hashList.ToArray());
        }

        public dynamic GetJsonDataForTimeRange(DateTime startTime, DateTime endTime, string name = "_emptyName_", string type = "_emptyType_", string subType = "_emptySubType_")
        {
            startTime = startTime.Truncate(TimeSpan.FromMinutes(1));
            endTime = endTime.Truncate(TimeSpan.FromMinutes(1));

            var redisDb = _redis.Value.GetDatabase();
            var quantileData = new QuantileData
            {
                Name = name,
                Type = type,
                SubType = subType,
                QuantileDurations = new Dictionary<DateTime, Tuple<int, int, int, int, int, int>>()
            };

            var dataPointName = name;
            var dataPointType = type;
            var dataPointSubType = subType;
            while (startTime < endTime)
            {
                var key = startTime.Ticks;
                var retrievedValue = redisDb.HashGet("urn:datapoints", key);
                if (retrievedValue.HasValue)
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int>>>>>(retrievedValue);
                    if (string.IsNullOrWhiteSpace(dataPointName) || dataPointName == "_emptyName_")
                    {
                        dataPointName = data.Keys.First(x => x != string.Empty && x.ToLower() != "_empytname_");
                        quantileData.Name = dataPointName;
                    }
                    if (string.IsNullOrWhiteSpace(dataPointType) || dataPointType == "_emptyType_")
                    {
                        dataPointType = data[dataPointName].Keys.First(x => x != string.Empty && x.ToLower() != "_emptyType_");
                        quantileData.Type = dataPointType;
                    }
                    if (string.IsNullOrWhiteSpace(dataPointSubType) || dataPointSubType == "_emptySubType_")
                    {
                        dataPointSubType =
                            data[dataPointName][dataPointType].Keys.First(x => x != string.Empty && x.ToLower() != "_emptySubType_");
                        quantileData.SubType = dataPointSubType;
                    }
                    try
                    {
                        quantileData.QuantileDurations[startTime] = data[dataPointName][dataPointType][dataPointSubType];
                    }
                    catch
                    {
                        quantileData.QuantileDurations[startTime] = Tuple.Create(0, 0, 0, 0, 0, 0);
                    }
                }

                startTime = startTime.AddMinutes(1);
            }

            return quantileData;
        }
    }
}
