namespace API.DTOs.Actitivities.Responses
{
    public record ActivityResponse(
            Guid Id,
            DateTime StartedAt,
            DateTime EndedAt,
            double DistanceMeters,
            int TotalTimeSeconds,
            int MovingTimeSeconds,
            double AverageSpeedKmh,
            double MaxSpeedKmh,
            double ElevationGainMeters
        );
}