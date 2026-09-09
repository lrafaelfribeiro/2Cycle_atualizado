using APP.Interfaces;
using APP.ViewModels;

namespace APP.Views
{
    public partial class RoutesView : ContentView, IRefreshableView
    {
        private readonly RoutesViewModel _viewModel;
        public RoutesView(RoutesViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        public Task RefreshAsync() => _viewModel.LoadCommand.ExecuteAsync(null);
    }
}