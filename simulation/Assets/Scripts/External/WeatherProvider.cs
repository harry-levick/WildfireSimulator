using Mapbox.Utils;
using Model;
using System.Net.Http;
using System.Threading.Tasks;
using UnityEngine;
using static Constants.StringConstants;

namespace External
{
    public class WeatherProvider
    {
        private static readonly HttpClient Client = new HttpClient();
        
        public async Task<Weather> GetWeatherReport(Vector2d latlon)
        {
            var response = 
                Client.GetAsync(string.Format(WeatherURL, latlon.x, latlon.y, WeatherAPIKey)).Result;
            response.EnsureSuccessStatusCode();
            var jsonString = await response.Content.ReadAsStringAsync();

            var jObject = JsonUtility.FromJson<Weather>(jsonString);
            return jObject;
        }
    }
}