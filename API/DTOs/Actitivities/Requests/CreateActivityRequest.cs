namespace API.DTOs.Actitivities.Requests
{
    public sealed record CreateActivityRequest(
            Guid Id,
            DateTime StartedAt,
            DateTime EndedAt,
            double DistanceMeters,
            int TotalTimeSeconds,
            int MovingTimeSeconds,
            double AverageSpeedKmh,
            double MaxSpeedKmh,
            double ElevationGainMeters,
            List<CreateTrackSegmentRequest> Segments
    );
}