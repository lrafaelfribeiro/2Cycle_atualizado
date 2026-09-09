using API.DTOs;
using FluentValidation;

namespace API.Validators.Auth
{
    public class LoginRequestValidator : AbstractValidator<LoginRequest>
    {
        public LoginRequestValidator()
        {
            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório.")
                .EmailAddress().WithMessage("Indique um email válido.");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("A password é obrigatoria.");
        }
    }
}