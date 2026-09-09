using APP.Services.Auth;
using APP.Services.Toast;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Text.RegularExpressions;

namespace APP.ViewModels
{
    public partial class RegisterViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IToastService _toastService;

        public RegisterViewModel(IAuthService authService, IToastService toastService)
        {
            _authService = authService;
            _toastService = toastService;
        }

        [ObservableProperty] private string name = string.Empty;
        [ObservableProperty] private string email = string.Empty;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private string password = string.Empty;

        [ObservableProperty] private bool isPasswordHidden = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        private bool acceptedTerms;

        [ObservableProperty] private bool hasMinLength;
        [ObservableProperty] private bool hasUppercase;
        [ObservableProperty] private bool hasSpecialChar;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(RegisterCommand))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy;

        private bool IsNotBusy => !IsBusy;

        [ObservableProperty] private string? errorMessage;

        // Mesma regra do RegisterRequestValidator do backend — mínimo 8, maiúscula, caractere especial
        private static readonly Regex SpecialCharPattern = new(@"[!@#$%^&*.,\-_]");

        partial void OnPasswordChanged(string value)
        {
            HasMinLength = value.Length >= 8;
            HasUppercase = value.Any(char.IsUpper);
            HasSpecialChar = SpecialCharPattern.IsMatch(value);
        }

        private bool IsPasswordValid => HasMinLength && HasUppercase && HasSpecialChar;

        [RelayCommand]
        private void TogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
        }

        [RelayCommand(CanExecute = nameof(CanRegister))]
        private async Task RegisterAsync(CancellationToken cancellationToken)
        {
            if (IsBusy) return;

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _authService.RegisterAsync(Name.Trim(), Email.Trim(), Password, cancellationToken);

                if (result.IsSuccess)
                {
                    Password = string.Empty;
                    await Shell.Current.GoToAsync("//create-profile");
                }
                else
                {
                    ErrorMessage = result.Error!.Message;
                    await _toastService.Show(ErrorMessage);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanRegister() =>
            !IsBusy &&
            !string.IsNullOrWhiteSpace(Name) &&
            !string.IsNullOrWhiteSpace(Email) &&
            IsPasswordValid &&
            AcceptedTerms;
    }
}
