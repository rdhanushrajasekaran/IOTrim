using System;
using System.Collections.Generic;
using System.Text;

namespace IOTrim.Model
{
    public class EMSModel
    {
        public DateTime LogTime { get; set; }
        public float L1_L2_Volt { get; set; }
        public float L2_L3_Volt { get; set; }
        public float L3_L1_Volt { get; set; }

        public float L1_N_Volt { get; set; }
        public float L2_N_Volt { get; set; }
        public float L3_N_Volt { get; set; }

        public float L1_C { get; set; }
        public float L2_C { get; set; }
        public float L3_C { get; set; }

        public float Power_Factor { get; set; }
        public float Frequency { get; set; }

        public float Active_Power { get; set; }
        public float Reactive_Power { get; set; }
        public float Apparent_Power { get; set; }

        public float Energy { get; set; }
        public float Ashift_Energy { get; set; }
        public float Bshift_Energy { get; set; }
        public float Cshift_Energy { get; set; }
        public float Total_Energy { get; set; }

        public int HourValue { get; set; }
    }
}
