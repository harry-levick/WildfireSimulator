using System.Collections.Generic;

public class WeatherModel
{
    public Wind current;
    public List<Wind> daily;
    public List<Wind> hourly;
    public float lat;
    public float lon;
    public string timezone;
    public string timezone_offset;
}

public class Wind
{
    public float wind_deg;
    public float wind_speed;
}
