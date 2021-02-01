using System;

namespace Model
{
    [Serializable]
    public class Wind
    {
        private const float MetreToFeet = 3.28084f;
        private const int MinuteToSecond = 60;
        public float wind_deg;
        public float wind_speed; // metres per second

        public float WindSpeedMetresPerSecond => wind_speed;

        public float WindSpeedFeetPerMinutes => (wind_speed * MetreToFeet) / MinuteToSecond;
    }
}

