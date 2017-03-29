using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
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

            ViewBag.DataPointNames = dataPointNames;

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

        private const string DataPointNamesUrn = "urn:datapointNames";

        private const string DurationQuantilesUrn = "urn:durationQuantiles";
    }
}
