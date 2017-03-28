using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;

namespace MdsDataAccessClientSample
{
    public static class MdsHelper
    {
        public static DataType ExtractDataType(string message)
        {
            var dataType = new DataType
            {
                Duration = 0,
                Name = string.Empty,
                Type = string.Empty,
                SubType = string.Empty
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
                    Name = newJObject["Name"].ToString(),
                    Type = newJObject["Type"].ToString(),
                    SubType = newJObject["SubType"].ToString()
                };
            }
            catch (Exception ex)
            {
                Console.WriteLine(message + "\n" + ex);
                Console.ReadKey();

                return dataType;
            }
        }

        public static string GetQuantileKey(DataType dataType)
        {
            return dataType.Name + MdsDataTypeDelimiter + dataType.Type + MdsDataTypeDelimiter + dataType.SubType;
        }

        public static void AppendCachedDurationQuantilesPerMinute(
            IDictionary<DateTime, IDictionary<string, Tuple<int, int, int, int, int, int>>>
                cachedDurationQuantilesPerMinute, IDictionary<string, List<int>> durationQuantiles, DateTime dateTime)
        {
            var quantilesToCache = new Dictionary<string, Tuple<int, int, int, int, int, int>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kv in durationQuantiles)
            {
                var sortedList = kv.Value;
                sortedList.Sort();
                var count = sortedList.Count;
                var quantileIndices = new List<int>();
                foreach (var quantile in _quantiles)
                {
                    var index = (int)(quantile * count);
                    quantileIndices.Add(index);
                }

                quantilesToCache[kv.Key] = new Tuple<int, int, int, int, int, int>(sortedList[quantileIndices[0]], sortedList[quantileIndices[1]], sortedList[quantileIndices[2]], sortedList[quantileIndices[3]], sortedList[quantileIndices[4]], sortedList[quantileIndices[5]]);
            }

            cachedDurationQuantilesPerMinute[dateTime] = quantilesToCache;
        }

        public static void AddQuantile(IDictionary<string, List<int>> durationQuantiles, DataType dataType)
        {
            var quantileKey = GetQuantileKey(dataType);
            List<int> quantileValues;
            if (!durationQuantiles.TryGetValue(quantileKey, out quantileValues))
            {
                quantileValues = new List<int>();
                durationQuantiles[quantileKey] = quantileValues;
            }

            quantileValues.Add(dataType.Duration);
        }

        private const string MdsDataTypeDelimiter = "~";

        private static readonly List<double> _quantiles = new List<double> { 0.5, 0.75, 0.9, 0.99, 0.999, 0.9995 };
    }
}
