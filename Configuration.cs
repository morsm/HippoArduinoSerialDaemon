using System;

using Newtonsoft.Json;

namespace Termors.Serivces.HippoArduinoSerialDaemon
{
    public class Configuration
    {
        [JsonProperty]
        public String Device { get; set; }

        [JsonProperty]
        public int Baudrate { get; set; }

        [JsonProperty]
        public double TempCalibration { get; set; }
    }
}
