using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
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

[Serializable]
public class Wind
{
    public float wind_deg;
    public float wind_speed;
}
