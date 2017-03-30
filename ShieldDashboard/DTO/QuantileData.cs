using System;
using System.Collections.Generic;

namespace ShieldDashboard.DTO
{
    public class QuantileData
    {
        public string Name { get; set; }

        public string Type { get; set; }

        public string SubType { get; set; }

        public List<QuantileDuration> QuantileDurations { get; set; }
    }

    public class QuantileDuration
    {
        public DateTime DateTime { get; set; }

        public Quantiles Quantiles { get; set; }
    }

    public class Quantiles
    {
        public int Item1 { get; set; }

        public int Item2 { get; set; }

        public int Item3 { get; set; }

        public int Item4 { get; set; }

        public int Item5 { get; set; }

        public int Item6 { get; set; }
    }
}