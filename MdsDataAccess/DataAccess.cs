using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using MdsDataAccess.Extensions;

namespace MdsDataAccess
{
    public class DataAccess
    {
        private readonly Lazy<ConnectionMultiplexer> _redis =
            new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("localhost:6379"));

        public void SaveData(IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int>>>>> durationQuantiles)
        {
            var redisDb = _redis.Value.GetDatabase();

            var hashList = new List<HashEntry>();
            foreach (var kv in durationQuantiles)
            {
                hashList.Add(new HashEntry(JsonConvert.SerializeObject(kv.Key.Truncate(TimeSpan.FromMinutes(1))), JsonConvert.SerializeObject(kv.Value)));
            }

            redisDb.HashSet("urn:datapoints", hashList.ToArray());
        }

        public dynamic GetData(DateTime dateTime)
        {
            var redisDb = _redis.Value.GetDatabase();

            var key = JsonConvert.SerializeObject(dateTime.Truncate(TimeSpan.FromMinutes(1)));
            var retrievedValue = redisDb.HashGet("urn:datapoints", key);

            return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int>>>>>(retrievedValue);
        }

        public dynamic GetDataForTimeRange(DateTime startTime, DateTime endTime)
        {
            startTime = startTime.Truncate(TimeSpan.FromMinutes(1));
            endTime = endTime.Truncate(TimeSpan.FromMinutes(1));

            var redisDb = _redis.Value.GetDatabase();
            var dataForTimeRange = new List<IDictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int>>>>>();
            while (startTime < endTime)
            {
                var key = JsonConvert.SerializeObject(endTime);
                var retrievedValue = redisDb.HashGet("urn:datapoints", key);
                if (retrievedValue.HasValue)
                {
                    dataForTimeRange.Add(JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int>>>>>(retrievedValue));
                }

                endTime = endTime.AddMinutes(-1);
            }

            return dataForTimeRange;
        }
    }
}
