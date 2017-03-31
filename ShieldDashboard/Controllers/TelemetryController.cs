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
            var lookBackHoursInt = int.Parse(lookBackHours);
            var endTime = DateTime.Parse(spotTime).Truncate(TimeSpan.FromMinutes(1));
            var startTime = endTime.AddHours(-lookBackHoursInt);

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
            var combinationFactor = (int)Math.Ceiling(lookBackHoursInt / 2.0);
            var counter = 1;
            var aggregatedQuantileDuration = new List<QuantileDuration>();
            while (startTime < endTime)
            {
                var key = startTime.Ticks;
                var retrievedValue = await redisDB.HashGetAsync(DurationQuantilesUrn, key);
                if (retrievedValue.HasValue)
                {
                    var data = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, Dictionary<string, Tuple<int, int, int, int, int, int, int>>>>>(retrievedValue);
                    try
                    {
                        var tupleValues = data[dataPointName][dataPointType][dataPointSubType];
                        aggregatedQuantileDuration.Add(new QuantileDuration
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
                        aggregatedQuantileDuration.Add(new QuantileDuration
                        {
                            DateTime = startTime,
                            Count = 0,
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

                    if (counter == combinationFactor)
                    {
                        quantileData.QuantileDurations.Add(new QuantileDuration
                        {
                            DateTime = aggregatedQuantileDuration[0].DateTime,
                            Count = aggregatedQuantileDuration.Select(x => x.Count).Sum(),
                            Quantiles = new Quantiles
                            {
                                Item1 = aggregatedQuantileDuration.Select(x => x.Quantiles.Item1).Sum() / combinationFactor,
                                Item2 = aggregatedQuantileDuration.Select(x => x.Quantiles.Item2).Sum() / combinationFactor,
                                Item3 = aggregatedQuantileDuration.Select(x => x.Quantiles.Item3).Sum() / combinationFactor,
                                Item4 = aggregatedQuantileDuration.Select(x => x.Quantiles.Item4).Sum() / combinationFactor,
                                Item5 = aggregatedQuantileDuration.Select(x => x.Quantiles.Item5).Sum() / combinationFactor,
                                Item6 = aggregatedQuantileDuration.Select(x => x.Quantiles.Item6).Sum() / combinationFactor
                            }
                        });

                        aggregatedQuantileDuration = new List<QuantileDuration>();
                        counter = 0;
                    }

                    counter++;
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
