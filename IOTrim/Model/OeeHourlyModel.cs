using System;
using System.Collections.Generic;
using System.Text;

namespace IOTrim.Model
{
    public class OeeHourlyModel
    {
        public string SNo { get; set; }
        public string Hour { get; set; }
        public string RunTime { get; set; }
        public string ManagementLoss { get; set; }
        public string Idle { get; set; }
        public string PlannedDowntime { get; set; }
        public string UnplannedDowntime { get; set; }
        public string GoodPart { get; set; }
        public string BadPart { get; set; }
        public string A { get; set; }
        public string P { get; set; }
        public string Q { get; set; }
        public string OEE { get; set; }

        public int HourValue { get; set; }
    }
}
