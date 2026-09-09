namespace API.DTOs
{
    public sealed record CreateProfileRequest
    (
        DateOnly BirthDate,
        double HeightCm,
        double InitialWeightKg,
        double TargetWeightKg,
        int ActivityDaysPerWeek,
        Sex Sex
    );

public sealed record UpdateProfileRequest
(
    DateOnly BirthDate,
    double HeightCm,
    double TargetWeightKg,
    int ActivityDaysPerWeek,
    Sex Sex
);
}
