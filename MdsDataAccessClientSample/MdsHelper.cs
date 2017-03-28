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

        public static string GetQuantileKey(DataType dataType)
        {
            return dataType.Name + MdsDataTypeDelimiter + dataType.Type + MdsDataTypeDelimiter + dataType.SubType;
        }

        public static void AppendCachedDurationQuantilesPerMinute(
            IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int>>>>>
                cachedDurationQuantilesPerMinute, IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> durationQuantiles, DateTime dateTime)
        {
            var quantilesToCache = new Dictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int>>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var kvName in durationQuantiles)
            {
                IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int>>> typeValue;
                if (!quantilesToCache.TryGetValue(kvName.Key, out typeValue))
                {
                    typeValue = new Dictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int>>>(StringComparer.OrdinalIgnoreCase);
                    quantilesToCache[kvName.Key] = typeValue;
                }
                foreach (var kvType in kvName.Value)
                {
                    IDictionary<string, Tuple<int, int, int, int, int, int>> subTypeValue;
                    if (!typeValue.TryGetValue(kvType.Key, out subTypeValue))
                    {
                        subTypeValue = new Dictionary<string, Tuple<int, int, int, int, int, int>>(StringComparer.OrdinalIgnoreCase);
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
                            new Tuple<int, int, int, int, int, int>(sortedList[quantileIndices[0]],
                                sortedList[quantileIndices[1]], sortedList[quantileIndices[2]],
                                sortedList[quantileIndices[3]], sortedList[quantileIndices[4]],
                                sortedList[quantileIndices[5]]);
                    }
                }
            }

            cachedDurationQuantilesPerMinute[dateTime] = quantilesToCache;
        }

        public static void AddQuantile(IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> durationQuantiles, DataType dataType)
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

        private const string MdsDataTypeDelimiter = "~";

        private static readonly List<double> _quantiles = new List<double> { 0.5, 0.75, 0.9, 0.99, 0.999, 0.9995 };

        private const string EmptyName = "_emptyName_";

        private const string EmptyType = "_emptyType_";

        private const string EmptySubType = "_emptySubType";
    }
}
