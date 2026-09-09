namespace APP.DTOs.Activities.Responses
{
    public record TrackPointResponse(
            double Latitude,
            double Longitude,
            double? AltitudeMeters,
            DateTime RecordedAt,
            int SegmentSequenceIndex);
}