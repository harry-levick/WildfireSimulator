namespace Constants
{
    public static class StringConstants
    {
        public const string ModelNumberURL =
            "http://127.0.0.1:5000/model-number?lat={0}&lon={1}";
        public const string ModelParametersURL =
            "http://127.0.0.1:5000/model-parameters?number={0}";
        public const string MoistureURL =
            "http://127.0.0.1:5500/live-fuel-moisture-content?lat={0}&lon={1}";
        public const string WeatherURL =
            "https://api.openweathermap.org/data/2.5/onecall?lat={0}&lon={1}&exclude=alerts,minutely,hourly,daily&appid={2}";
        public const string WeatherAPIKey = "aceb15f37444868d75c58b9ef6033fc8";

        public const string WindArrowPrefab = "Prefabs/WindArrow";
    }
}