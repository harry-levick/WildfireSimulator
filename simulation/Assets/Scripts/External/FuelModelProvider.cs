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
        
        private static async Task<int> GetFuelModelNumber(Vector2d latlon)
        {
            var response = Client.GetAsync(string.Format(ModelNumberURL, latlon.x, latlon.y)).Result;
            response.EnsureSuccessStatusCode();
            var modelNumber = await response.Content.ReadAsStringAsync();
            return int.Parse(modelNumber);
        }

        public async Task<Fuel> GetFuelModelParameters(Vector2d latlon)
        {
            var modelNumber = await GetFuelModelNumber(latlon);
            var response = Client.GetAsync(string.Format(ModelParametersURL, modelNumber)).Result;
            response.EnsureSuccessStatusCode();

            return JsonUtility.FromJson<Fuel>(await response.Content.ReadAsStringAsync());
        }
    }
}