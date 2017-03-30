using System.Collections.Concurrent;
using MdsDataAccess;
using MdsDataAccessClientSample;
using Microsoft.Cis.Monitoring.Mds.mdscommon;

namespace MdsDataAccessClientLibSample
{
    using Microsoft.Cis.Monitoring.DataAccess;
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class MdsDataAccessClientSample
    {
        #region Asynchronous APIs Sample

        public static void FetechDataUsingAsyncApi()
        {
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
                            var extractedDataType = MdsHelper.ExtractDataType(item["Message"].ToString());
                            MdsHelper.AddQuantile(_durationQuantiles, extractedDataType);
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

                MdsHelper.AppendCachedDurationQuantilesPerMinute(_cachedDurationQuantilesPerMinute, _durationQuantiles, startTime);
                MdsHelper.AppendListOfDataPoints(_durationQuantiles, _dataPointNames);
                _dataAccess.SaveData(_cachedDurationQuantilesPerMinute);

                var retrievedData = _dataAccess.GetData(startTime);

                startTime = startTime.AddMinutes(1);
                _durationQuantiles = new Dictionary<string, IDictionary<string, IDictionary<string, List<int>>>>(StringComparer.OrdinalIgnoreCase);
                _cachedDurationQuantilesPerMinute =
                    new Dictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>>();
            }

            _dataAccess.SaveDatapointNames(_dataPointNames);
            Console.ReadKey();
        }

        private static IDictionary<string, IDictionary<string, IDictionary<string, List<int>>>> _durationQuantiles =
            new Dictionary<string, IDictionary<string, IDictionary<string, List<int>>>>(StringComparer.OrdinalIgnoreCase);

        private static readonly IDictionary<string, IDictionary<string, HashSet<string>>> _dataPointNames =
            new Dictionary<string, IDictionary<string, HashSet<string>>>(StringComparer.OrdinalIgnoreCase);

        private static IDictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>> _cachedDurationQuantilesPerMinute =
            new Dictionary<DateTime, IDictionary<string, IDictionary<string, IDictionary<string, Tuple<int, int, int, int, int, int, int>>>>>();


        #endregion

        #region Get MDS table names using GetTables API

        static void GetMdsTables()
        {
            var mdsAllTableNamesRegExp = "OfficeLicensingInt.*";
            var nLatestVersions = 3;
            var mdsDataAccessClient = new MdsDataAccessClient(MdsEndpoint, MdsCertSubjectName);

            IEnumerable<string> tables = mdsDataAccessClient.GetTables(mdsAllTableNamesRegExp, nLatestVersions);

            Console.WriteLine("Found {0} tables : ", tables.Count());
            foreach (var tn in tables)
            {
                Console.WriteLine(tn);
            }
            Console.ReadKey();
            Console.WriteLine();
        }

        #endregion

        public static void Main(string[] args)
        {
            Console.WriteLine("1. Fetching data from MDS");
            var items = _dataAccess.GetDataForTimeRange(DateTime.UtcNow.AddDays(-2),
                DateTime.UtcNow.AddDays(-2).AddHours(1));

            var quantileDataItems = _dataAccess.GetJsonDataForTimeRange(DateTime.UtcNow.AddHours(-6), DateTime.UtcNow);
            FetechDataUsingAsyncApi();

            //Console.WriteLine("2. Fetching MDS tables that matches the reg expression using MdsDataAccessClient.GetTables");
            //GetMdsTables();
        }

        private const string MdsCertSubjectName =
            "CN=Claret-Int-Mds, OU=Office, O=Microsoft, L=Redmond, S=Washington, C=US";

        private const string MdsEndpoint = "https://firstparty.monitoring.windows.net";

        private const string MdsUlsTraceEventTableNameRegex = "OfficeLicensingIntUlsTraceEvents.*";

        private const string QueryString = "where uls_EventId == \"ardak\" and (Message.Contains(\"API\") or Message.Contains(\"Storage\") or Message.Contains(\"Service\")) and not Message.Contains(\"PingTest\")";

        private const int MaxRetry = 6;

        private static readonly DataAccess _dataAccess = new DataAccess();
    }
}
