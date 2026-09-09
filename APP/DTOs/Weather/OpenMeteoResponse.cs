using System.Text.Json.Serialization;

namespace APP.DTOs.Weather
{
    public class OpenMeteoResponse
    {
        [JsonPropertyName("hourly")]
        public HourlyData Hourly { get; set; } = new();
    }

    public class HourlyData
    {
        [JsonPropertyName("time")]
        public List<string> Time { get; set; } = new();

        [JsonPropertyName("temperature_2m")]
        public List<double> Temperature2m { get; set; } = new();

        [JsonPropertyName("weathercode")]
        public List<int> WeatherCode { get; set; } = new();
    }
}
