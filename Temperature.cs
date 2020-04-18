using System;
namespace Termors.Serivces.HippoArduinoSerialDaemon
{
    public struct Temperature
    {
        public double TempCelsius { get; set; }

        public double RelHumidity { get; set; }
    }
}
