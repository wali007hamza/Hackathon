using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using ShieldDashboard.DTO;
using ShieldDashboard.Extensions;
using StackExchange.Redis;

namespace ShieldDashboard.Controllers
{
    public class TelemetryController : Controller
    {
        public async Task<ActionResult> Index()
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var redisDB = redis.GetDatabase();

            var retrievedDataPointNames = await redisDB.HashGetAllAsync(DataPointNamesUrn);
            var dataPointNames =
                new Dictionary<string, IDictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in retrievedDataPointNames)
            {
                dataPointNames.Add(entry.Name.ToString(), JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(entry.Value));
            }

            var activityNames = dataPointNames.Keys.OrderByDescending(x => x).ToList();
            ViewBag.ActivityNames = activityNames;
            ViewBag.FirstActivity = new QuantileData
            {
                Name = activityNames[0],
                Type = dataPointNames[activityNames[0]].Keys.First(),
                SubType = dataPointNames[activityNames[0]].Values.First().First()
            };

            return View();
        }

        public async Task<ActionResult> GetActivityNames()
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var redisDB = redis.GetDatabase();

            var retrievedDataPointNames = await redisDB.HashGetAllAsync(DataPointNamesUrn);
            var dataPointNames =
                new Dictionary<string, IDictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in retrievedDataPointNames)
            {
                dataPointNames.Add(entry.Name.ToString(), JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(entry.Value));
            }

            return Json(dataPointNames.Keys, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetActivityTypesForName(string activityName)
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var redisDB = redis.GetDatabase();

            var retrievedDataPointNames = await redisDB.HashGetAllAsync(DataPointNamesUrn);
            var dataPointNames =
                new Dictionary<string, IDictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in retrievedDataPointNames)
            {
                dataPointNames.Add(entry.Name.ToString(), JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(entry.Value));
            }

            return Json(dataPointNames[activityName].Keys, JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetActivitySubTypesForNameAndType(string activityName, string activityType)
        {
            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var redisDB = redis.GetDatabase();

            var retrievedDataPointNames = await redisDB.HashGetAllAsync(DataPointNamesUrn);
            var dataPointNames =
                new Dictionary<string, IDictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);
            foreach (var entry in retrievedDataPointNames)
            {
                dataPointNames.Add(entry.Name.ToString(), JsonConvert.DeserializeObject<Dictionary<string, HashSet<string>>>(entry.Value));
            }

            return Json(dataPointNames[activityName][activityType].ToArray(), JsonRequestBehavior.AllowGet);
        }

        public async Task<ActionResult> GetQuantileDurations(string spotTime,
            string lookBackHours,
            string activityName,
            string activityType,
            string activitySubType)
        {
            var endTime= DateTime.Parse(spotTime).Truncate(TimeSpan.FromMinutes(1));
            var startTime = endTime.AddHours(-int.Parse(lookBackHours));

            var redis = ConnectionMultiplexer.Connect("localhost:6379");
            var redisDB = redis.GetDatabase();

            var quantileData = new QuantileData
            {
                Name = activityName,
                Type = activityType,
                SubType = activitySubType,
                QuantileDurations = new List<QuantileDuration>()
            };

            var dataPointName = activityName;
            var dataPointType = activityType;
            var dataPointSubType = activitySubType;
            while (startTime < endTime)
            {
                var key = startTime.Ticks;
                var retrievedValue = await redisDB.HashGetAsync(DurationQuantilesUrn, key);
                if (retrievedValue.HasValue)
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int, int>>>>>(retrievedValue);
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
                        var tupleValues = data[dataPointName][dataPointType][dataPointSubType];
                        quantileData.QuantileDurations.Add(new QuantileDuration
                        {
                            DateTime = startTime,
                            Count = tupleValues.Item1,
                            Quantiles = new Quantiles
                            {
                                Item1 = tupleValues.Item2,
                                Item2 = tupleValues.Item3,
                                Item3 = tupleValues.Item4,
                                Item4 = tupleValues.Item5,
                                Item5 = tupleValues.Item6,
                                Item6 = tupleValues.Item7
                            }
                        });

                    }
                    catch
                    {
                        quantileData.QuantileDurations.Add(new QuantileDuration
                        {
                            DateTime = startTime,
                            Quantiles = new Quantiles
                            {
                                Item1 = 0,
                                Item2 = 0,
                                Item3 = 0,
                                Item4 = 0,
                                Item5 = 0,
                                Item6 = 0
                            }
                        });
                    }
                }

                startTime = startTime.AddMinutes(1);
            }

            var jsonQuantileData = JsonConvert.SerializeObject(quantileData);
            return Json(jsonQuantileData, JsonRequestBehavior.AllowGet);
        }

        private const string DataPointNamesUrn = "urn:datapointNames";

        private const string DurationQuantilesUrn = "urn:durationQuantiles";
    }
}
