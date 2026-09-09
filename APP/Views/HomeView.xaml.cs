using APP.Interfaces;
using APP.ViewModels;

namespace APP.Views;


public partial class HomeView : ContentView, INavigableView
{
    public event EventHandler<string>? TabRequested;
    public HomeView(HomeViewModel viewModel)
    {
        InitializeComponent();
        BindingContext = viewModel;
        _ = viewModel.LoadGreetingCommand.ExecuteAsync(null);
        _ = viewModel.RefreshWeeklyStatisticsCommand.ExecuteAsync(null);
    }

    private void OnStartActivityClicked(object sender, EventArgs e)
    {
        TabRequested?.Invoke(this, "startActivity");
    }

    private async void OnRoutesTapped(object sender, TappedEventArgs e)
    {
        var view = (VisualElement)sender;
        await view.FadeToAsync(0.6, 80);
        await view.FadeToAsync(1, 80);
    }

    private async void OnActivitiesTapped(object sender, TappedEventArgs e)
    {
        TabRequested?.Invoke(this, "activities");
    }
}