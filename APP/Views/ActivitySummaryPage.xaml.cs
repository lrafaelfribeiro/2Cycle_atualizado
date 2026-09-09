using APP.Controls;
using APP.Extensions;
using APP.ViewModels;

namespace APP.Views;

public partial class ActivitySummaryPage : ContentPage
{
    private readonly ActivitySummaryViewModel _viewModel;
    private readonly SpeedChartDrawable _speedChartDrawable = new();

    public ActivitySummaryPage(ActivitySummaryViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        SpeedChartView.Drawable = _speedChartDrawable;
        _viewModel.DataLoaded += OnDataLoaded;
    }

    private void OnDataLoaded(object? sender, EventArgs e)
    {
        MainThread.BeginInvokeOnMainThread(() => LoadMapAndChartAsync().FireAndForgetSafe());
    }

    private async Task LoadMapAndChartAsync()
    {
        try
        {
            if (_viewModel.RoutePoints.Count >= 2)
            {
                RouteMapWebView.Source = new HtmlWebViewSource
                {
                    Html = await RouteMapHtmlBuilder.BuildStaticRouteHtml(_viewModel.RoutePoints.ToLatLongTuples())
                };
            }

            _speedChartDrawable.Samples = _viewModel.SpeedSamples;
            SpeedChartView.Invalidate();
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Falha ao carregar mapa/gráfico: {ex}");
        }
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.DataLoaded -= OnDataLoaded;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
        => await Shell.Current.GoToAsync("..");
}