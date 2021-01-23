using System;
using System.Collections.Generic;

namespace Assets.Scripts.Model
{
    [Serializable]
    public class Weather
    {
        public Wind current;
        public List<Wind> daily;
        public List<Wind> hourly;
        public float lat;
        public float lon;
        public string timezone;
        public string timezone_offset;
    }
}
