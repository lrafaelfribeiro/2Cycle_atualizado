namespace API.DTOs.Actitivities.Responses
{
    public record TrackPointResponse(
            double Latitude,
            double Longitude,
            double? AltitudeMeters,
            DateTime RecordedAt,
            int SegmentIndex
        );
}