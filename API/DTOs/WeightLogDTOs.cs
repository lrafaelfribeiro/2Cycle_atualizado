namespace API.DTOs
{
    public sealed record AddWeightLogRequest
    (
        double WeightKg,
        DateTime? RecordedAt
    );

    public record WeightLogResponse
    (
        double WeightKg,
        DateTime RecordedAt
    );
}
