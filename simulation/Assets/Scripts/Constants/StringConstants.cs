using System.Collections.Generic;

namespace Constants
{
    public static class StringConstants
    {
        private const string LocalHost = "http://127.0.0.1";
        private const string ModelPort = ":5000";
        private const string MoisturePort = ":5500";
        
        public const string ModelNumberURL =
            LocalHost + ModelPort + "/model-number?lat={0}&lon={1}";
        public const string ModelParametersURL =
            LocalHost + ModelPort + "/model-parameters?number={0}";
        public const string PutControlLineUrl =
            LocalHost + ModelPort + "/control-lines/{0}&{1}&{2}&{3}";
        public const string ClearControlLinesUrl =
            LocalHost + ModelPort + "/control-lines/clear";
        public const string MoistureURL =
            LocalHost + MoisturePort + "/live-fuel-moisture-content?lat={0}&lon={1}";
        
        public const string WeatherURL =
            "https://api.openweathermap.org/data/2.5/onecall?lat={0}&lon={1}&exclude=alerts,minutely,hourly,daily&appid={2}";
        public const string WeatherAPIKey = "aceb15f37444868d75c58b9ef6033fc8";

        public const string WindArrowPrefab = "Prefabs/WindArrow";
        public const string ControlLinePrefab = "Prefabs/ControlLine";

        public static readonly List<string> NonBurnableCodes = new List<string> { "NB0", "NB1", "NB2", "NB3", "NB8", "NB9" };
    }
}