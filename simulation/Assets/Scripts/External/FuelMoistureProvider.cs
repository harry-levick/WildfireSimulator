using Mapbox.Utils;
using System.Globalization;
using System.Net.Http;
using System.Threading.Tasks;
using static Constants.StringConstants;

namespace External
{
    public class FuelMoistureProvider
    {
        private static readonly HttpClient Client = new HttpClient();
        
        public async Task<double> GetFuelMoistureContent(Vector2d latlon)
        {
            var response = Client.GetAsync(string.Format(MoistureURL, latlon.x, latlon.y)).Result;
            response.EnsureSuccessStatusCode();
            return
                double.Parse(
                    await response.Content.ReadAsStringAsync(),
                    CultureInfo.InvariantCulture.NumberFormat
                ) / 100; // divide by 100 to get percentage fuel moisture content.
        }
    }
}