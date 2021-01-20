using System;
using System.Collections.Generic;

namespace Assets.Scripts.Models
{
    [Serializable]
    public class WeatherModel
    {
        public WindModel current;
        public List<WindModel> daily;
        public List<WindModel> hourly;
        public float lat;
        public float lon;
        public string timezone;
        public string timezone_offset;
    }
}
