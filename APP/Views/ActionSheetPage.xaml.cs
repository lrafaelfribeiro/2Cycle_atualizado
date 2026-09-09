using APP.Components;

namespace APP.Views;

public partial class ActionSheetPage : ContentPage
{
    private const double DismissThreshold = 120;
    private const uint EntranceAnimationDurationMs = 380;
    private const uint ExitAnimationDurationMs = 200;
    private const uint SnapBackAnimationDurationMs = 150;
    private const uint OverlayFadeDurationMs = 260;

    private readonly TaskCompletionSource<string?> _resultSource = new();

    private double _lastDragY;
    private bool _isClosing;

    public ActionSheetPage(IReadOnlyList<ActionSheetOption> options)
    {
        InitializeComponent();
        OptionsHost.BindingContext = options;
    }

    public static async Task<string?> ShowAsync(IReadOnlyList<ActionSheetOption> options)
    {
        var normalizedOptions = MarkLastOption(options);
        var page = new ActionSheetPage(normalizedOptions);

        await Shell.Current.Navigation.PushModalAsync(page, animated: false);
        await page.PlayEntranceAnimationAsync();

        var result = await page._resultSource.Task;

        await Shell.Current.Navigation.PopModalAsync(false);

        return result;
    }

    // Garante que só a última opção fica sem divisor a seguir, sem exigir
    // que quem chama ShowAsync se preocupe com isso.
    private static IReadOnlyList<ActionSheetOption> MarkLastOption(IReadOnlyList<ActionSheetOption> options)
    {
        return options
            .Select((o, i) => new ActionSheetOption
            {
                IconSource = o.IconSource,
                Text = o.Text,
                ResultKey = o.ResultKey,
                IsDestructive = o.IsDestructive,
                IsLast = i == options.Count - 1
            })
            .ToList();
    }

    private async Task PlayEntranceAnimationAsync()
    {
        await Task.WhenAll(
            Overlay.FadeToAsync(1, OverlayFadeDurationMs),
            SlideSheetInAsync());
    }

    private async Task SlideSheetInAsync()
    {
        RootLayout.Opacity = 1;
        await RootLayout.TranslateToAsync(0, 0, EntranceAnimationDurationMs, Easing.CubicOut);
    }

    private async Task CloseWithResultAsync(string? result)
    {
        if (_isClosing)
            return;

        _isClosing = true;

        await RootLayout.TranslateToAsync(0, 500, ExitAnimationDurationMs, Easing.CubicIn);
        _resultSource.TrySetResult(result);
    }

    private void OnBackgroundTapped(object? sender, TappedEventArgs e) => _ = CloseWithResultAsync(null);
    private void OnCancelClicked(object? sender, EventArgs e) => _ = CloseWithResultAsync(null);

    private void OnOptionTapped(object? sender, EventArgs e)
    {
        if (sender is BindableObject { BindingContext: ActionSheetOption option })
        {
            _ = CloseWithResultAsync(option.ResultKey);
        }
    }

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
                    _ = RootLayout.TranslateToAsync(0, 0, SnapBackAnimationDurationMs, Easing.CubicOut);
                }
                _lastDragY = 0;
                break;
        }
    }
}