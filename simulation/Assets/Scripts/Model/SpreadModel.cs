using System;

public class SpreadModel
{
    public float spreadRate; // the rate of spread in ft/min
    public float spreadBearing; // the direction of spread as a bearing.

    public SpreadModel(float rate, float bearing)
    {
        spreadRate = rate;
        spreadBearing = bearing;
    }
}
