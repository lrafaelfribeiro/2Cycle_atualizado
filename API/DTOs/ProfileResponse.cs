namespace API.DTOs
{
    public record ProfileResponse
    (
        string Name,
        DateOnly BirthDate,
        double HeightCm,
        double? CurrentWeightKg,
        double TargetWeightKg,
        int ActivityDaysPerWeek,
        Sex Sex,
        double? Bmi
    );
}
