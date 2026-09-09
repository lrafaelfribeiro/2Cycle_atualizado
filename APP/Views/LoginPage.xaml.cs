using APP.ViewModels;

namespace APP.Views;

public partial class LoginPage : ContentPage
{
    public LoginPage(LoginViewModel loginViewModel)
    {
        InitializeComponent();
        BindingContext = loginViewModel;

        loginViewModel.PropertyChanged += async (s, e) =>
        {
            if (e.PropertyName == nameof(LoginViewModel.ErrorMessage) && loginViewModel.ErrorMessage != null)
            {
                await ErrorLabel.FadeToAsync(1, 100);
            }
        };
    }

    private void EmailEntry_Completed(object sender, EventArgs e)
    {
        // Dar foco na password quando clicar no next do email
        PasswordEntry.Focus();
    }
}