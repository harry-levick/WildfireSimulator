using System.Net.Http;
using System.Threading.Tasks;
using Mapbox.Utils;
using Model;
using UnityEngine;
using static Constants.StringConstants;

namespace External
{
    public class FuelModelProvider
    {
        private static readonly HttpClient Client = new HttpClient();
        
        private static async Task<int> GetFuelModelNumber(Vector2d latlon, string uuid)
        {
            var response = Client.GetAsync(string.Format(ModelNumberURL, latlon.x, latlon.y, uuid)).Result;
            response.EnsureSuccessStatusCode();
            var modelNumber = await response.Content.ReadAsStringAsync();
            return int.Parse(modelNumber);
        }

        public async Task<Fuel> GetFuelModelParameters(Vector2d latlon, string uuid)
        {
            var modelNumber = await GetFuelModelNumber(latlon, uuid);
            var response = Client.GetAsync(string.Format(ModelParametersURL, modelNumber)).Result;
            response.EnsureSuccessStatusCode();

            return JsonUtility.FromJson<Fuel>(await response.Content.ReadAsStringAsync());
        }

        /// <summary>
        /// </summary>
        /// <param name="min">contains the minimum values for latitude (x) and longitude (y)</param>
        /// <param name="max">contains the maximum values for latitude (x) and longitude (y)</param>
        public static void PutControlLine(Vector2d min, Vector2d max, string uuid)
        {
            var response = Client.PutAsync(string.Format(PutControlLineUrl, min.x, max.x, min.y, max.y, uuid), null).Result;
            response.EnsureSuccessStatusCode();
        }

        public static void ClearControlLines(string uuid)
        {
            var response = Client.GetAsync(string.Format(ClearControlLinesUrl, uuid)).Result;
            response.EnsureSuccessStatusCode();
        }

        public static void ClearAllControlLines()
        {
            var response = Client.GetAsync(ClearAllControlLinesUrl).Result;
            response.EnsureSuccessStatusCode();
        }
    }
}