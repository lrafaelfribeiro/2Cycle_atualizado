using APP.Interfaces;
using APP.ViewModels;
using APP.ViewModels.Activities;

namespace APP.Views;

public partial class StartActivityView : ContentView, IRefreshableView
{
    private readonly StartActivityViewModel _viewModel;

    public StartActivityView(StartActivityViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;
    }

    protected override void OnParentSet()
    {
        base.OnParentSet();
        _ = _viewModel.InitializeAsync();
    }

    public Task RefreshAsync() => _viewModel.InitializeAsync();

}
