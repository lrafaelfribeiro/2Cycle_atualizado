using API.DTOs;
using FluentValidation;

namespace API.Validators.Profile
{
    public class CreateProfileRequestValidator : AbstractValidator<CreateProfileRequest>
    {
        public CreateProfileRequestValidator()
        {
            RuleFor(x => x.BirthDate)
                .NotEmpty().WithMessage("Data de Nascimento é obrigatória")
                .Must(dob => dob <= DateOnly.FromDateTime(DateTime.UtcNow.AddYears(-13)))
                .WithMessage("Deves ter pelo menos 13 anos")
                .Must(dob => dob >= new DateOnly(1900, 1, 1))
                .WithMessage("Data de nascimento inválida");

            RuleFor(x => x.HeightCm)
                .GreaterThan(0).WithMessage("A altura não pode ser negativa")
                .LessThanOrEqualTo(300).WithMessage("A altura deve ser realista");

            RuleFor(x => x.InitialWeightKg)
                .GreaterThan(0).WithMessage("O peso não pode ser negativo.")
                .LessThanOrEqualTo(600).WithMessage("O peso deve ser realista");

            RuleFor(x => x.TargetWeightKg)
                .GreaterThan(0).WithMessage("O peso pretendido não pode ser negativo.")
                .LessThanOrEqualTo(600).WithMessage("O peso pretendido deve ser realista");

            RuleFor(x => x.ActivityDaysPerWeek)
                .InclusiveBetween(0, 7).WithMessage("Dias de atividade deve estar entre 0 e 7");
            RuleFor(x => x.Sex).IsInEnum();
        }
    }


}
