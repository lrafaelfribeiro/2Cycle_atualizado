namespace APP.Views;

public partial class ActivityOptionsPage : ContentPage
{
    private const double DismissThreshold = 120;

    private readonly TaskCompletionSource<string?> _resultSource = new();

    private double _lastDragY;
    private bool _isClosing;

    public ActivityOptionsPage()
    {
        InitializeComponent();
    }

    public static async Task<string?> ShowAsync()
    {
        var page = new ActivityOptionsPage();

        await Shell.Current.Navigation.PushModalAsync(
            page,
            animated: false);

        await page.PlayEntranceAnimationAsync();

        var result = await page._resultSource.Task;

        await Shell.Current.Navigation.PopModalAsync(false);

        return result;
    }


    private async Task PlayEntranceAnimationAsync()
    {
        await Task.WhenAll(
            Overlay.FadeToAsync(1, 260),
            SlideSheetInAsync()
        );
    }

    private async Task SlideSheetInAsync()
    {
        RootLayout.Opacity = 1;

        await RootLayout.TranslateToAsync(
            0,
            0,
            380,
            Easing.CubicOut);
    }

    private async Task CloseWithResultAsync(string? result)
    {
        if (_isClosing)
            return;

        _isClosing = true;

        await RootLayout.TranslateToAsync(0, 500, 200, Easing.CubicIn);
        _resultSource.TrySetResult(result);
    }

    private void OnBackgroundTapped(object? sender, TappedEventArgs e) => _ = CloseWithResultAsync(null);
    private void OnCancelClicked(object? sender, EventArgs e) => _ = CloseWithResultAsync(null);
    private void OnEditTapped(object? sender, EventArgs e) => _ = CloseWithResultAsync("Editar");
    private void OnShareTapped(object? sender, EventArgs e) => _ = CloseWithResultAsync("Partilhar");
    private void OnDuplicateTapped(object? sender, EventArgs e) => _ = CloseWithResultAsync("Duplicar");
    private void OnDeleteTapped(object? sender, EventArgs e) => _ = CloseWithResultAsync("Eliminar");

    private void OnPanUpdated(object? sender, PanUpdatedEventArgs e)
    {
        switch (e.StatusType)
        {
            case GestureStatus.Running:
                if (e.TotalY > 0)
                {
                    RootLayout.TranslationY = e.TotalY;
                    _lastDragY = e.TotalY;
                }
                break;

            case GestureStatus.Completed:
            case GestureStatus.Canceled:
                if (_lastDragY > DismissThreshold)
                {
                    _ = CloseWithResultAsync(null);
                }
                else
                {
                    _ = RootLayout.TranslateToAsync(0, 0, 150, Easing.CubicOut);
                }
                _lastDragY = 0;
                break;
        }
    }
}