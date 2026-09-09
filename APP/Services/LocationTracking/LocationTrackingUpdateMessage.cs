namespace APP.Services.LocationTracking
{
    public record LocationTrackingUpdateMessage(
        double DistanceMeters,
        double CurrentSpeedKmh,
        int MovingTimeSeconds,
        double? AltitudeMeters,
        double ElevationGainMeters,
        double Latitude,
        double Longitude
    );

    public record LocationTrackingAutoPauseChangedMessage(bool IsAutoPaused);

    // Publicado quando o Service termina/aborta sozinho (ex: permissão revogada a meio)
    public record LocationTrackingStoppedMessage(Guid ActivityId, bool Success);
}
