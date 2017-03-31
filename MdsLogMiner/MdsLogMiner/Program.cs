using Microsoft.Cis.Monitoring.DataAccess;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Threading.Tasks;

namespace MdsLogMiner
{
    public class Program
    {
        public static void Main(string[] args)
        {
            Console.WriteLine("Fetching data from MDS");
            FetchDataUsingAsyncApi().Wait();
        }

        public static async Task FetchDataUsingAsyncApi()
        {
            //string certfile = System.IO.Path.Combine(Environment.ExpandEnvironmentVariable‌​s("%HOME%"), @"site\wwwroot\HttpTriggerCSharp4\myCertFile.pfx");
            //var certFile = @"C:\MyRepo\Hackathon\MdsCertificate.pfx";
            //var cert = new X509Certificate2(certFile, "P@ssword");
            //var uri = new UriBuilder(MdsEndpoint);
            //var mdsDataAccessClient = new MdsDataAccessClient(uri.Uri, cert);
            var mdsDataAccessClient = new MdsDataAccessClient(MdsEndpoint, MdsCertSubjectName);
            int retryNum = 0, counter = 0;

            var startTime = DateTime.UtcNow.AddHours(-2);
            while (startTime < DateTime.UtcNow)
            {
                while (retryNum < MaxRetry)
                {
                    try
                    {
                        counter = 0;
                        var result = mdsDataAccessClient.QueryMdsTableAsync(MdsUlsTraceEventTableNameRegex, 0, startTime,
                            startTime.AddMinutes(1), QueryString);

                        foreach (var item in result)
                        {
                            var extractedDataType = ExtractDataType(item["Message"].ToString());
                            AddQuantile(_durationQuantiles, extractedDataType);
                            Console.WriteLine(item["Message"]);
                        }

                        break;
                    }
                    catch (Exception e)
                    {
                        Console.Write(e.ToString());
                        Console.ReadKey();
                        Console.WriteLine("counter = " + counter);
                        System.Threading.Thread.Sleep(5000);
                        retryNum++;
                    }
                }

                AppendCachedDurationQuantilesPerMinute(_cachedDurationQuantilesPerMinute, _durationQuantiles, startTime);
                AppendListOfDataPoints(_durationQuantiles, _dataPointNames);
                await SaveDataAsync(_cachedDurationQuantilesPerMinute);

                startTime = startTime.AddMinutes(1);
                _durationQuantiles = new Dictionary<string, IDictionary<string, IDictionary<string, List<int>>>>(StringComparer.OrdinalIgnoreCase);
                _cachedDurationQuantilesPerMinute =
                    new Dictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>>();
            }

            SaveDatapointNames(_dataPointNames);
            Console.ReadKey();
        }

        private class DataType
        {
            public string Name { get; set; }

            public string Type { get; set; }

            public string SubType { get; set; }

            public int Duration { get; set; }
        }

        public static async Task SaveDataAsync(IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>> durationQuantiles)
        {
            //var redisDb = _redis.Value.GetDatabase();
            var redisDb = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings["RedisCacheUrl"]).GetDatabase();

            var hashList = new List<HashEntry>();
            var hashDeletionList = new List<string>();
            var retentionTimeInDays = int.Parse(ConfigurationManager.AppSettings["RetentionTimeInDays"]);
            foreach (var kv in durationQuantiles)
            {
                var dateTimeKey = kv.Key.AddTicks(-(kv.Key.Ticks % TimeSpan.FromMinutes(1).Ticks));
                hashList.Add(new HashEntry(dateTimeKey.Ticks, JsonConvert.SerializeObject(kv.Value)));
                hashList.Add(new HashEntry(dateTimeKey.Ticks, JsonConvert.SerializeObject(kv.Value)));
                hashDeletionList.Add(dateTimeKey.AddDays(-retentionTimeInDays).Ticks.ToString());
            }

            await redisDb.HashSetAsync(DurationQuantilesUrn, hashList.ToArray());
            foreach (var entryToDelete in hashDeletionList)
            {
                await redisDb.HashDeleteAsync(DurationQuantilesUrn, entryToDelete);
            }
        }

        public static void SaveDatapointNames(IDictionary<string, IDictionary<string, HashSet<string>>> dataPointNames)
        {
            //var redisDB = _redis.Value.GetDatabase();
            var redisDb = ConnectionMultiplexer.Connect(ConfigurationManager.AppSettings["RedisCacheUrl"]).GetDatabase();
            var hashList = new List<HashEntry>();
            foreach (var kv in dataPointNames)
            {
                hashList.Add(new HashEntry(kv.Key, JsonConvert.SerializeObject(kv.Value)));
            }

            redisDb.HashSet(DatapointNamesUrn, hashList.ToArray());
        }

        private static DataType ExtractDataType(string message)
        {
            var dataType = new DataType
            {
                Duration = 0,
                Name = EmptyName,
                Type = EmptyType,
                SubType = EmptySubType
            };
            try
            {
                var jObject = JObject.Parse(message);
                var item = jObject["DC"];

                if (item == null)
                {
                    return dataType;
                }

                var jsonString = item.Value<string>().Replace("\\", "");
                var newJObject = JObject.Parse(jsonString);

                return new DataType
                {
                    Duration = DateTime.Parse(jObject["DT"].ToString()).Millisecond,
                    Name = !string.IsNullOrWhiteSpace(newJObject["Name"].ToString()) ? newJObject["Name"].ToString() : EmptyName,
                    Type = !string.IsNullOrWhiteSpace(newJObject["Type"].ToString()) ? newJObject["Type"].ToString() : EmptyType,
                    SubType = !string.IsNullOrWhiteSpace(newJObject["SubType"].ToString()) ? newJObject["SubType"].ToString() : EmptySubType
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(message + "\n" + ex);
                Console.ReadKey();

                return dataType;
            }
        }

        private static void AppendCachedDurationQuantilesPerMinute(
            IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>>
                cachedDurationQuantilesPerMinute, IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> durationQuantiles, DateTime dateTime)
        {
            var quantilesToCache = new Dictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvName in durationQuantiles)
            {
                IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>> typeValue;
                if (!quantilesToCache.TryGetValue(kvName.Key, out typeValue))
                {
                    typeValue = new Dictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>(StringComparer.OrdinalIgnoreCase);
                    quantilesToCache[kvName.Key] = typeValue;
                }
                foreach (var kvType in kvName.Value)
                {
                    IDictionary<string, Tuple<int, int, int, int, int, int, int>> subTypeValue;
                    if (!typeValue.TryGetValue(kvType.Key, out subTypeValue))
                    {
                        subTypeValue = new Dictionary<string, Tuple<int, int, int, int, int, int, int>>(StringComparer.OrdinalIgnoreCase);
                        typeValue[kvType.Key] = subTypeValue;
                    }
                    foreach (var kvSubType in kvType.Value)
                    {
                        var sortedList = kvSubType.Value;
                        sortedList.Sort();
                        var count = sortedList.Count;
                        var quantileIndices = new List<int>();
                        foreach (var quantile in _quantiles)
                        {
                            var index = (int)(quantile * count);
                            quantileIndices.Add(index);
                        }

                        subTypeValue[kvSubType.Key] =
                            new Tuple<int, int, int, int, int, int, int>(count, sortedList[quantileIndices[0]],
                                sortedList[quantileIndices[1]], sortedList[quantileIndices[2]],
                                sortedList[quantileIndices[3]], sortedList[quantileIndices[4]],
                                sortedList[quantileIndices[5]]);
                    }
                }
            }

            cachedDurationQuantilesPerMinute[dateTime] = quantilesToCache;
        }

        private static void AddQuantile(IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> durationQuantiles, DataType dataType)
        {
            IDictionary<string, IDictionary<string, List<int>>> nameValue;
            if (!durationQuantiles.TryGetValue(dataType.Name, out nameValue))
            {
                nameValue = new Dictionary<string, IDictionary<string, List<int>>>(StringComparer.OrdinalIgnoreCase);
                durationQuantiles[dataType.Name] = nameValue;
            }

            IDictionary<string, List<int>> typeValue;
            if (!nameValue.TryGetValue(dataType.Type, out typeValue))
            {
                typeValue = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
                nameValue[dataType.Type] = typeValue;
            }

            List<int> subTypeValue;
            if (!typeValue.TryGetValue(dataType.SubType, out subTypeValue))
            {
                subTypeValue = new List<int>();
                typeValue[dataType.SubType] = subTypeValue;
            }

            subTypeValue.Add(dataType.Duration);
        }

        private static void AppendListOfDataPoints(
            IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> durationQuantiles,
            IDictionary<string, IDictionary<string, HashSet<string>>> dataPointNames)
        {
            foreach (var kvName in durationQuantiles)
            {
                IDictionary<string, HashSet<string>> activityName;
                if (!dataPointNames.TryGetValue(kvName.Key, out activityName))
                {
                    activityName = new Dictionary<string, HashSet<string>>(StringComparer.OrdinalIgnoreCase);
                    dataPointNames[kvName.Key] = activityName;
                }

                foreach (var kvType in kvName.Value)
                {
                    HashSet<string> activityType;
                    if (!activityName.TryGetValue(kvType.Key, out activityType))
                    {
                        activityType = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                        activityName[kvType.Key] = activityType;
                    }

                    foreach (var kvSubType in kvType.Value)
                    {
                        activityType.Add(kvSubType.Key);
                    }
                }
            }
        }

        #region Private Fields

        private static IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> _durationQuantiles =
            new Dictionary<string, IDictionary<string, IDictionary<string, List<int>>>>(StringComparer.OrdinalIgnoreCase);

        private static readonly IDictionary<string, IDictionary<string, HashSet<string>>> _dataPointNames =
            new Dictionary<string, IDictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);

        private static IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>> _cachedDurationQuantilesPerMinute =
            new Dictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>>();

        private readonly Lazy<ConnectionMultiplexer> _redis =
            new Lazy<ConnectionMultiplexer>(() => ConnectionMultiplexer.Connect("localhost:6379"));

        private const string MdsCertSubjectName =
            "CN=Claret-Int-Mds, OU=Office, O=Microsoft, L=Redmond, S=Washington, C=US";

        private const string MdsEndpoint = "https://firstparty.monitoring.windows.net";

        private const string MdsUlsTraceEventTableNameRegex = "OfficeLicensingIntUlsTraceEvents.*";

        private const string QueryString = "where uls_EventId == \"ardak\" and (Message.Contains(\"API\") or Message.Contains(\"Storage\") or Message.Contains(\"Service\")) and not Message.Contains(\"PingTest\")";

        private const int MaxRetry = 6;

        private static readonly List<double> _quantiles = new List<double> { 0.5, 0.75, 0.9, 0.99, 0.999, 0.9995 };

        private const string EmptyName = "_emptyName_";

        private const string EmptyType = "_emptyType_";

        private const string EmptySubType = "_emptySubType_";

        private const string DurationQuantilesUrn = "urn:durationQuantiles";

        private const string DatapointNamesUrn = "urn:datapointNames";

        #endregion
    }
}
