using API.DTOs;
using FluentValidation;

namespace API.Validators.Auth
{
    public class RegisterRequestValidator : AbstractValidator<RegisterRequest>
    {
        public RegisterRequestValidator()
        {
            RuleFor(x => x.Name)
                .NotEmpty().WithMessage("O nome é obrigatório")
                .Length(2, 100).WithMessage("O nome deve ter entre 2 e 100 caracteres.")
                .Matches(@"^[\p{L}\s'-]+$").WithMessage("O nome so deve conter letras.");

            RuleFor(x => x.Email)
                .NotEmpty().WithMessage("O email é obrigatório")
                .EmailAddress().WithMessage("Digite um email valido");

            RuleFor(x => x.Password)
                .NotEmpty().WithMessage("A password é obrigatória.")
                .Matches("^(?=.*[A-Z])(?=.*[!@#$%^&*.,\\-_]).{8,}$").WithMessage("Deve ter pelo menos 8 caracters, uma letra maiuscula e um caracter especial.");
        }
    }
}
