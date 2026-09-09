using APP.Services.Auth;
using APP.Services.Toast;
using APP.Services.Token;
using APP.Views;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using System.Globalization;

namespace APP.ViewModels
{
    public partial class LoginViewModel : ObservableObject
    {
        private readonly IAuthService _authService;
        private readonly IToastService _toastService;

        public LoginViewModel(IAuthService authService, IToastService toastService)
        {
            _authService = authService;
            _toastService = toastService;
        }

        [ObservableProperty]
        private string email = string.Empty;

        [ObservableProperty]
        private string password = string.Empty;

        [ObservableProperty]
        private bool isPasswordHidden = true;

        [ObservableProperty]
        [NotifyCanExecuteChangedFor(nameof(LoginCommand))]
        [NotifyPropertyChangedFor(nameof(IsNotBusy))]
        private bool isBusy = false;

        private bool IsNotBusy => !IsBusy;

        [ObservableProperty]
        private string? errorMessage;

        [RelayCommand]
        private void TogglePassword()
        {
            IsPasswordHidden = !IsPasswordHidden;
        }

        [RelayCommand(CanExecute = nameof(CanLogin))]
        private async Task LoginAsync(CancellationToken cancellationToken)
        {
            if (IsBusy)
            {
                return;
            }

            IsBusy = true;
            ErrorMessage = null;

            try
            {
                var result = await _authService.LoginAsync(Email, Password, cancellationToken);

                if (result.IsSuccess)
                {
                    IsBusy = false;
                    Password = string.Empty;
                    await Shell.Current.GoToAsync("//main");
                }
                else
                {
                    IsBusy = false;
                    ErrorMessage = result.Error!.Message;
                    await _toastService.Show(ErrorMessage);
                }
            }
            finally
            {
                IsBusy = false;
            }
        }

        private bool CanLogin() => !IsBusy;

        [RelayCommand]
        private async Task GoToRegisterAsync()
        {
            await Shell.Current.GoToAsync(nameof(RegisterPage));
        }
    }
}
