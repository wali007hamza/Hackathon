using MdsDataAccessClientSample;

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

            var startTime = DateTime.UtcNow.AddDays(-2);
            while (startTime < DateTime.UtcNow)
            {
                var endTime = startTime.AddSeconds(IntervalInSeconds);
                while (retryNum < MaxRetry)
                {
                    try
                    {
                        counter = 0;
                        var result = mdsDataAccessClient.QueryMdsTableAsync(MdsUlsTraceEventTableNameRegex, 0, startTime,
                            endTime, QueryString);

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
                        Console.WriteLine("counter = " + counter);
                        System.Threading.Thread.Sleep(5000);
                        retryNum++;
                    }
                }

                MdsHelper.AppendCachedDurationQuantilesPerMinute(_cachedDurationQuantilesPerMinute, _durationQuantiles, startTime);
                startTime = endTime;
                _durationQuantiles = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
            }

            Console.ReadKey();
        }

        private static IDictionary<string, List<int>> _durationQuantiles = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);

        private static readonly IDictionary<DateTime, IDictionary<string, Tuple<int, int, int, int, int, int>>> _cachedDurationQuantilesPerMinute =
            new Dictionary<DateTime, IDictionary<string, Tuple<int, int, int, int, int, int>>>();

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

        private const int IntervalInSeconds = 60;
    }
}
