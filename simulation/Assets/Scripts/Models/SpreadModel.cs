using System;

namespace Assets.Scripts.Models
{
    public class SpreadModel
    {
        public double spreadRate; // the rate of spread in ft/min
        public double spreadBearing; // the direction of spread as a bearing.

        public SpreadModel(double rate, double bearing)
        {
            spreadRate = rate;
            spreadBearing = bearing;
        }
    }
}
