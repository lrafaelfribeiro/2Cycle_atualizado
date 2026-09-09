using API.DTOs;
using FluentValidation;

namespace API.Validators.Profile.Weight
{
    public class AddWeightLogRequestValidator : AbstractValidator<AddWeightLogRequest>
    {
        public AddWeightLogRequestValidator()
        {
            RuleFor(x => x.WeightKg)
                .GreaterThan(0).WithMessage("O peso não pode ser negativo.")
                .LessThanOrEqualTo(600).WithMessage("O peso deve ser realista");

            RuleFor(x => x.RecordedAt)
                .Must(date => date is null || date <= DateTime.UtcNow)
                .WithMessage("A data não pode ser no futuro");
        }
    }
}