using APP.Controls;
using APP.Core;
using APP.Extensions;
using APP.ViewModels;

namespace APP.Views;

public partial class RouteDetailPage : ContentPage
{
    private readonly RouteDetailViewModel _viewModel;
    private readonly ElevationChartDrawable _elevationChartDrawable = new();

    public RouteDetailPage(RouteDetailViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        ElevationChartView.Drawable = _elevationChartDrawable;
        _viewModel.DataLoaded += OnDataLoaded;
    }

    private void OnDataLoaded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => LoadMapAndChartAsync().FireAndForgetSafe());
    }

    // Wrapper explícito: torna o "fire-and-forget" seguro, apanhando qualquer exceção
    // em vez de a deixar propagar como async void sem handler.
    private async Task LoadMapAndChartAsync()
    {
        try
        {
            RouteMapWebView.Source = new HtmlWebViewSource
            {
                Html = await RouteMapHtmlBuilder.BuildStaticRouteHtml(_viewModel.RoutePoints)
            };

            _elevationChartDrawable.Profile = _viewModel.ElevationProfile;
            ElevationChartView.Invalidate();
        }
        catch (Exception ex)
        {
            // TODO: mesmo padrão de log/erro que uses no resto da app.
            System.Diagnostics.Debug.WriteLine($"Falha ao carregar mapa/gráfico: {ex}");
        }
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.DataLoaded -= OnDataLoaded;
    }
}