using System;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web.Configuration;
using Newtonsoft.Json.Linq;

namespace ChesterMeatMatrix.Helpers
{
    public static class OpenRouteServiceHelper
    {
        private static readonly string ApiKey = WebConfigurationManager.AppSettings["OpenRouteServiceApiKey"];

        public static bool IsConfigured => !string.IsNullOrEmpty(ApiKey) && ApiKey != "ey***Y2Zj***0=";

        public static async Task<GeocodeResult> GeocodeAddressAsync(string address)
        {
            if (!IsConfigured || string.IsNullOrWhiteSpace(address))
                return null;

            try
            {
                using (var client = new HttpClient())
                {
                    var encodedAddress = Uri.EscapeDataString(address);
                    var url = $"https://api.openrouteservice.org/geocode/search?api_key={ApiKey}&text={encodedAddress}&boundary.country=ZA&size=1";

                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    var json = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    var features = obj["features"];

                    if (features == null || !features.HasValues)
                        return null;

                    var geometry = features[0]["geometry"];
                    var coordinates = geometry["coordinates"];

                    return new GeocodeResult
                    {
                        Longitude = (double)coordinates[0],
                        Latitude = (double)coordinates[1]
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Geocoding error: {ex.Message}");
                return null;
            }
        }

        public static async Task<RouteResult> GetRouteAsync(double startLat, double startLon, double endLat, double endLon)
        {
            if (!IsConfigured)
                return null;

            try
            {
                using (var client = new HttpClient())
                {
                    var url = $"https://api.openrouteservice.org/v2/directions/driving-car?api_key={ApiKey}&start={startLon},{startLat}&end={endLon},{endLat}";

                    var response = await client.GetAsync(url);
                    if (!response.IsSuccessStatusCode)
                        return null;

                    var json = await response.Content.ReadAsStringAsync();
                    var obj = JObject.Parse(json);
                    var features = obj["features"];

                    if (features == null || !features.HasValues)
                        return null;

                    var properties = features[0]["properties"];
                    var segments = properties["segments"][0];

                    return new RouteResult
                    {
                        DistanceKm = (double)segments["distance"] / 1000,
                        DurationMinutes = (double)segments["duration"] / 60
                    };
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Route calculation error: {ex.Message}");
                return null;
            }
        }
    }

    public class GeocodeResult
    {
        public double Latitude { get; set; }
        public double Longitude { get; set; }
    }

    public class RouteResult
    {
        public double DistanceKm { get; set; }
        public double DurationMinutes { get; set; }
    }
}