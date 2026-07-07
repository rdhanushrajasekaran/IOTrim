using System;
using System.Collections.Generic;
using System.Text;

namespace IOTrim.Model
{
    public class DataModel
    {

        public DateTime dateTime;
        // Identification & Basic Info
        public string BLSerialNo { get; set; }           // 1 
        public string HolderSerialNo { get; set; }       // 2
        public string FixtureNumber { get; set; }        // 14
        public string StationName { get; set; }          // 15
        public string Shift { get; set; }                // 13

        // Screwing Parameters
        public float ScrewingTorque { get; set; }        // 3
        public int ScrewingAngle { get; set; }           // 4
        public bool ScrewingResult { get; set; }         // 7

        // Pressing & Inspection
        public int BLPressingLoad { get; set; }          // 5
        public bool ProfiloMeterResult { get; set; }       // 6
        public float ProfiloMeterHeight { get; set; }    // 10

        // Coating & Color
        public string Variant { get; set; }          // 8
        public string Color { get; set; }                // 9

        // Count & Result
        public int Count { get; set; }                   // 11
        public string Result { get; set; }               // 12

    }
}
