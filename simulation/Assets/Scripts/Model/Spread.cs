using System;

namespace Model
{
    public class Spread
    {
        public readonly double SpreadRateFeetPerMin; // the rate of spread in ft/min
        public readonly double SpreadBearing; // the direction of spread as a bearing.

        public Spread(double rateFeetPerMin, double bearing)
        {
            SpreadRateFeetPerMin = rateFeetPerMin;
            SpreadBearing = bearing;
        }
    }
}
