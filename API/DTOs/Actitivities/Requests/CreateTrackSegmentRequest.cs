namespace API.DTOs.Actitivities.Requests
{
    public sealed record CreateTrackSegmentRequest(
            int SequenceIndex,
            DateTime StartedAt,
            DateTime EndedAt,
            List<CreateTrackPointRequest> Points
    );
}