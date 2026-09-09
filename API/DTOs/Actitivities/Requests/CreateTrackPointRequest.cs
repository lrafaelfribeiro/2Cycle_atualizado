namespace API.DTOs.Actitivities.Requests
{
    public sealed record CreateTrackPointRequest(
            int Sequence,
            double Latitude,
            double Longitude,
            double? AltitudeMeters,
            DateTime RecordedAt
        );
}