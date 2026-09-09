using APP.Interfaces;
using APP.ViewModels;

namespace APP.Views;

public partial class ProfileView : ContentView, IRefreshableView
{
    private readonly ProfileViewModel _viewModel;

    public ProfileView(ProfileViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _ = _viewModel.LoadCommand.ExecuteAsync(null);
    }

    public Task RefreshAsync() => _viewModel.LoadCommand.ExecuteAsync(null);
}