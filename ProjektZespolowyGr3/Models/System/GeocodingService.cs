using Newtonsoft.Json.Linq;
using System.Diagnostics;
using System.Globalization;

namespace ProjektZespolowyGr3.Models.System
{
    public interface IGeocodingService
    {
        Task<(double Longitude, double Latitude)?> GetAddressLocation(string address);
        int CalculateDistanceKm(double Longitude1, double Latitude1, double Longitude2, double Latitude2);
    }

    public class GeocodingService : IGeocodingService
    {
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public GeocodingService(IConfiguration configuration)
        {
            _configuration = configuration;
            _httpClient = CreateHttpClient();
        }

        // Source - https://stackoverflow.com/a/65882200
        // Posted by Juan G Carmona, modified by community. See post 'Timeline' for change history
        // Retrieved 2026-05-06, License - CC BY-SA 4.0

        private HttpClient CreateHttpClient()
        {
            var client = new HttpClient();

            var email = _configuration["Nominatim:ContactEmail"];

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                $"ProjektZespolowyGr3/1.0 ({email})"
            );

            return client;
        }

        public async Task<(double Longitude, double Latitude)?> GetAddressLocation(string address)
        {
            try
            {
                var targetUrl =
                    $"https://nominatim.openstreetmap.org/search" +
                    $"?q={Uri.EscapeDataString(address)}" +
                    $"&format=json&limit=1";

                using var response = await _httpClient.SendAsync(
                    new HttpRequestMessage(HttpMethod.Get, targetUrl),
                    HttpCompletionOption.ResponseHeadersRead
                );

                Debug.WriteLine($"Geocoding request URL: {targetUrl}");
                Debug.WriteLine($"Geocoding response status: {response.StatusCode}");

                if (!response.IsSuccessStatusCode)
                    return null;

                var content = await response.Content.ReadAsStringAsync();
                var json = JArray.Parse(content).FirstOrDefault();

                Debug.WriteLine($"Geocoding response content: {content}");

                if (json == null)
                    return null;

                double latitude = double.Parse(
                    json["lat"]!.ToString(),
                    CultureInfo.InvariantCulture
                );

                double longitude = double.Parse(
                    json["lon"]!.ToString(),
                    CultureInfo.InvariantCulture
                );

                return (Longitude: longitude, Latitude: latitude);
            }
            catch
            {
                throw;
            }
        }

        public int CalculateDistanceKm(
            double Longitude1,
            double Latitude1,
            double Longitude2,
            double Latitude2
        )
        {
            var R = 6371;
            var dLat = ToRadians(Latitude2 - Latitude1);
            var dLon = ToRadians(Longitude2 - Longitude1);
            var a =
                Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(Latitude1)) *
                    Math.Cos(ToRadians(Latitude2)) *
                    Math.Sin(dLon / 2) *
                    Math.Sin(dLon / 2);
            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            var distance = R * c;
            return (int)Math.Round(distance);
        }

        private double ToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
    }
}