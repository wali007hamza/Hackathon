using Newtonsoft.Json;
using StackExchange.Redis;
using System;
using System.Collections.Generic;

namespace MdsDataAccess
{
    public class DataAccess
    {
        private readonly Lazy<ConnectionMultiplexer> _redis =
            new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("localhost:6379"));

        public void SaveData(IDictionary<DateTime, IDictionary<string, Tuple<int, int, int, int, int, int>>> durationQuantiles)
        {
            var redisDb = _redis.Value.GetDatabase();

            var hashList = new List<HashEntry>();
            foreach (var kv in durationQuantiles)
            {
                hashList.Add(new HashEntry(JsonConvert.SerializeObject(kv.Key), JsonConvert.SerializeObject(kv.Value)));
            }

            redisDb.HashSet("urn:datapoints", hashList.ToArray());
        }

        public dynamic GetData(DateTime dateTime)
        {
            var redisDb = _redis.Value.GetDatabase();

            var key = JsonConvert.SerializeObject(dateTime);
            var retrievedValue = redisDb.HashGet("urn:datapoints", key);

            return JsonConvert.DeserializeObject<Dictionary<string, Tuple<int, int, int, int, int, int>>>(retrievedValue);
        }
    }
}
