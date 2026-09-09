using APP.ViewModels;

namespace APP.Views;

public partial class CreateProfilePage : ContentPage
{
    public CreateProfilePage(CreateProfileViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;

        DatePickerBirth.MaximumDate = DateTime.Today;
    }
}