using APP.ViewModels;

namespace APP.Views;

public partial class RegisterPage : ContentPage
{
    public RegisterPage(RegisterViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
    }

    private void NameEntry_Completed(object sender, EventArgs e) => EmailEntry.Focus();
    private void EmailEntry_Completed(object sender, EventArgs e) => PasswordEntry.Focus();

    private async void LoginLink_Clicked(object sender, EventArgs e)
    {
        await Shell.Current.GoToAsync("..");
    }
}