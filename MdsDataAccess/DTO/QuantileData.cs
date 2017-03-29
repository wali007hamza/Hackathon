using System;
using System.Collections.Generic;

namespace MdsDataAccess.DTO
{
    public class QuantileData
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string SubType { get; set; }

        public IDictionary<DateTime, Tuple<int, int, int, int, int, int>> QuantileDurations { get; set; }
    }
}
