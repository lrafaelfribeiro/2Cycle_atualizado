using APP.Models;

namespace APP.Services.Weather
{
    public interface IWeatherService
    {
        Task<List<HourlyForecastItem>> GetForecastAsync(double latitude, double longitude, int hoursAhead = 3);
    }
}
