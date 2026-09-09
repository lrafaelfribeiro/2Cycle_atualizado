using System;
using System.Collections.Generic;
using System.Text;

namespace APP.DTOs
{
    public sealed record SyncActivityRequest(
        Guid Id,
        DateTime StartedAt,
        DateTime EndedAt,
        double DistanceMeters,
        int TotalTimeSeconds,
        int MovingTimeSeconds,
        double AverageSpeedKmh,
        double MaxSpeedKmh,
        double ElevationGainMeters,
        List<SyncTrackSegmentRequest> Segments
    );

    public sealed record SyncTrackSegmentRequest(
        int SequenceIndex,
        DateTime StartedAt,
        DateTime EndedAt,
        List<SyncTrackPointRequest> Points
    );

    public sealed record SyncTrackPointRequest(
        int Sequence,
        double Latitude,
        double Longitude,
        double? AltitudeMeters,
        DateTime RecordedAt
    );
}
