using API.DTOs.Actitivities.Requests;
using FluentValidation;

namespace API.Validators.Activity
{
    public class CreateTrackPointRequestValidator : AbstractValidator<CreateTrackPointRequest>
    {
        public CreateTrackPointRequestValidator()
        {
            RuleFor(x => x.Latitude)
                .Must(double.IsFinite)
                .WithMessage("A latitude deve ser um valor válido.")
                .InclusiveBetween(-90, 90);

            RuleFor(x => x.Longitude)
                .Must(double.IsFinite)
                .WithMessage("A longitude deve ser um valor válido.")
                .InclusiveBetween(-180, 180);

            RuleFor(x => x.Sequence)
                .GreaterThanOrEqualTo(0);

            RuleFor(x => x.AltitudeMeters)
                .Must(altitude => !altitude.HasValue || double.IsFinite(altitude.Value))
                .WithMessage("A altitude deve ser um valor válido.");

            RuleFor(x => x.AltitudeMeters)
                .GreaterThanOrEqualTo(-500)
                .LessThanOrEqualTo(10_000)
                .When(x => x.AltitudeMeters.HasValue);
        }
    }
}