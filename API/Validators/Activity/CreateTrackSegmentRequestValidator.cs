using API.DTOs.Actitivities.Requests;
using FluentValidation;

namespace API.Validators.Activity
{
    public class CreateTrackSegmentRequestValidator : AbstractValidator<CreateTrackSegmentRequest>
    {
        public CreateTrackSegmentRequestValidator()
        {
            RuleFor(x => x.EndedAt)
                .GreaterThanOrEqualTo(x => x.StartedAt);

            RuleFor(x => x.SequenceIndex)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.Points)
                .NotEmpty()
                .WithMessage("Um segmento precisa de pelo menos um ponto.")
                .Must(points => points.Count <= ActivityValidationLimits.MaxPointsPerSegment)
                .WithMessage($"Um segmento não pode ter mais de {ActivityValidationLimits.MaxPointsPerSegment} pontos.");

            RuleForEach(x => x.Points)
                .SetValidator(new CreateTrackPointRequestValidator());
        }
    }
}