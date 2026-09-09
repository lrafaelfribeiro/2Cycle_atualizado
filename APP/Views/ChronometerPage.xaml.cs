using System.ComponentModel;
using APP.ViewModels;

namespace APP.Views;

public partial class ChronometerPage : ContentPage
{
    private readonly TrackingViewModel _viewModel;
    private double _headerHeight = -1;

    public ChronometerPage(TrackingViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = _viewModel;

        // captura a altura real do cabeçalho assim que ele é medido
        HeaderGrid.SizeChanged += (_, _) =>
        {
            if (_headerHeight <= 0 && HeaderGrid.Height > 0)
                _headerHeight = HeaderGrid.Height;
        };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        _viewModel.OnAppearing();
        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        _viewModel.OnDisappearing();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(TrackingViewModel.HeaderVisible))
            AnimateHeader(_viewModel.HeaderVisible);
    }

    private void AnimateHeader(bool show)
    {
        if (_headerHeight <= 0)
        {
            // segurança: se ainda não medimos a altura real, faz o toggle direto
            HeaderGrid.IsVisible = show;
            return;
        }

        if (show)
        {
            HeaderGrid.IsVisible = true;

            var animation = new Animation(v =>
            {
                HeaderGrid.HeightRequest = v;
                HeaderGrid.Opacity = v / _headerHeight;
            }, 0, _headerHeight);

            animation.Commit(this, "HeaderShow", length: 220, easing: Easing.CubicOut);
        }
        else
        {
            var animation = new Animation(v =>
            {
                HeaderGrid.HeightRequest = v;
                HeaderGrid.Opacity = v / _headerHeight;
            }, _headerHeight, 0);

            animation.Commit(this, "HeaderHide", length: 220, easing: Easing.CubicIn,
                finished: (_, __) => HeaderGrid.IsVisible = false);
        }
    }

    protected override bool OnBackButtonPressed()
    {
        if (_viewModel.IsTracking)
        {
            _ = StopTrackingAsync();
            return true; // impede o pop imediato — nós tratamos da navegação
        }

        return base.OnBackButtonPressed();
    }

    private async Task StopTrackingAsync()
    {
        if (_viewModel.StopCommand.CanExecute(null))
            await _viewModel.StopCommand.ExecuteAsync(null);
    }
}