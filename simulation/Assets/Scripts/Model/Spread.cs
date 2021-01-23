using System;

namespace Assets.Scripts.Model
{
    public class Spread
    {
        public double spreadRate; // the rate of spread in ft/min
        public double spreadBearing; // the direction of spread as a bearing.

        public Spread(double rate, double bearing)
        {
            spreadRate = rate;
            spreadBearing = bearing;
        }
    }
}
