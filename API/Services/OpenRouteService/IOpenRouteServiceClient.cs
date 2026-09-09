namespace API.Services.OpenRouteService
{
    public record ORSCoordinate(double Latitude, double Longitude);
    public record ORSRoutePoint(double Latitude, double Longitude, double? AltitudeMeters);
    public record ORSRouteResult(double DistanceMeters, List<ORSRoutePoint> Points);

    public interface IOpenRouteServiceClient
    {
        Task<ORSRouteResult> GetCyclingRouteAsync(
            IReadOnlyList<ORSCoordinate> coordinates,
            CancellationToken cancellationToken);

        Task<ORSRouteResult> GetRoundTripRouteAsync(
            double originLat, double originLon,
            double desiredLengthMeters,
            CancellationToken cancellationToken);
    }
}