using API.DTOs.Actitivities.Requests;
using FluentValidation;

namespace API.Validators.Activity
{
    public class CreateActivityRequestValidator : AbstractValidator<CreateActivityRequest>
    {
        public CreateActivityRequestValidator()
        {
            RuleFor(x => x.Id)
                .NotEmpty();

            RuleFor(x => x.EndedAt)
                .GreaterThanOrEqualTo(x => x.StartedAt)
                .WithMessage("A data de fim não pode ser anterior à data de início.");

            RuleFor(x => x.DistanceMeters)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(ActivityValidationLimits.MaxDistanceMeters)
                .WithMessage($"A distância não pode exceder {ActivityValidationLimits.MaxDistanceMeters / 1_000:0} km.");

            RuleFor(x => x.TotalTimeSeconds)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(ActivityValidationLimits.MaxTotalTimeSeconds)
                .WithMessage("O tempo total não pode exceder 24 horas.");

            RuleFor(x => x.AverageSpeedKmh)
                .Must(double.IsFinite)
                .WithMessage("A velocidade média deve ser um valor válido.")
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(ActivityValidationLimits.MaxAverageSpeedKmh)
                .WithMessage($"A velocidade média não pode exceder {ActivityValidationLimits.MaxAverageSpeedKmh} km/h.");

            RuleFor(x => x.MaxSpeedKmh)
                .Must(double.IsFinite)
                .WithMessage("A velocidade máxima deve ser um valor válido.")
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(ActivityValidationLimits.MaxSpeedKmh)
                .WithMessage($"A velocidade máxima não pode exceder {ActivityValidationLimits.MaxSpeedKmh} km/h.");

            RuleFor(x => x.ElevationGainMeters)
                .Must(double.IsFinite)
                .WithMessage("O ganho de elevação deve ser um valor válido.")
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(ActivityValidationLimits.MaxElevationGainMeters)
                .WithMessage($"O ganho de elevação não pode exceder {ActivityValidationLimits.MaxElevationGainMeters / 1_000:0} km.");

            RuleFor(x => x.MovingTimeSeconds)
                .GreaterThanOrEqualTo(0)
                .LessThanOrEqualTo(x => x.TotalTimeSeconds)
                .WithMessage("O tempo em movimento não pode exceder o tempo total.");

            RuleFor(x => x.Segments)
                .NotEmpty()
                .WithMessage("Uma atividade precisa de pelo menos um segmento.")
                .Must(segments => segments.Count <= ActivityValidationLimits.MaxSegments)
                .WithMessage($"Uma atividade não pode ter mais de {ActivityValidationLimits.MaxSegments} segmentos.");

            RuleFor(x => x.Segments)
                .Must(HasValidTotalPointCount)
                .WithMessage($"Uma atividade não pode ter mais de {ActivityValidationLimits.MaxTotalPoints} pontos no total.");

            RuleForEach(x => x.Segments)
                .SetValidator(new CreateTrackSegmentRequestValidator());
        }

        private static bool HasValidTotalPointCount(
            IList<CreateTrackSegmentRequest> segments)
        {
            return segments.Sum(segment => segment.Points.Count) <= ActivityValidationLimits.MaxTotalPoints;
        }
    }
}