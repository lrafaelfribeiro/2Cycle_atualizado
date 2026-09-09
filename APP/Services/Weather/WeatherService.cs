using APP.Core;
using APP.DTOs.Weather;
using APP.Models;
using System.Globalization;
using System.Net.Http.Json;

namespace APP.Services.Weather
{
    public class WeatherService : IWeatherService
    {
        private readonly HttpClient _httpClient;

        public WeatherService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<List<HourlyForecastItem>> GetForecastAsync(double latitude, double longitude, int hoursAhead = 3)
        {
            string lat = latitude.ToString(CultureInfo.InvariantCulture);
            string lon = longitude.ToString(CultureInfo.InvariantCulture);

            string url = $"v1/forecast?latitude={lat}&longitude={lon}&hourly=temperature_2m,weathercode&forecast_days=2&timezone=auto";

            var response = await _httpClient.GetFromJsonAsync<OpenMeteoResponse>(url);
            if (response is null || response.Hourly.Time.Count == 0)
                return new List<HourlyForecastItem>();

            var now = DateTime.Now;

            int startIndex = 0;
            for (int i = 0; i < response.Hourly.Time.Count; i++)
            {
                if (DateTime.Parse(response.Hourly.Time[i]) <= now)
                    startIndex = i;
                else
                    break;
            }

            var items = new List<HourlyForecastItem>();
            int lastIndex = Math.Min(startIndex + hoursAhead, response.Hourly.Time.Count - 1);

            for (int i = startIndex; i <= lastIndex; i++)
            {
                var time = DateTime.Parse(response.Hourly.Time[i]);
                var (emoji, description) = WeatherCodeMapper.Map(response.Hourly.WeatherCode[i]);

                items.Add(new HourlyForecastItem
                {
                    TimeLabel = i == startIndex ? "Agora" : time.ToString("HH:mm"),
                    TemperatureDisplay = $"{response.Hourly.Temperature2m[i]:F0}°C",
                    Icon = emoji,
                    Description = description
                });
            }

            return items;
        }
    }
}
