using APP.Interfaces;
using APP.ViewModels;

namespace APP.Views;

public partial class ActivitiesView : ContentView, IRefreshableView
{
    private readonly ActivitiesViewModel _viewModel;

    public ActivitiesView(ActivitiesViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
        _ = _viewModel.LoadCommand.ExecuteAsync(null);
    }

    public Task RefreshAsync() => _viewModel.LoadCommand.ExecuteAsync(null);
}