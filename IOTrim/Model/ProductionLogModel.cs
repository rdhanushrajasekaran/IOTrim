using System;

namespace IOTrim.Model
{
    internal class ProductionLogModel
    {
        public DateTime LogDateTime { get; set; }

        public float RunTime { get; set; }
        public float ManagementLoss { get; set; }
        public float Idle { get; set; }
        public float PlannedDowntime { get; set; }
        public float UnplannedDowntime { get; set; }

        public int GoodPart { get; set; }
        public int BadPart { get; set; }

        public float A { get; set; }
        public float P { get; set; }
        public float Q { get; set; }
        public float OEE { get; set; }
    }
}