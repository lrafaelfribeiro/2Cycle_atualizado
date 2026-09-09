using APP.Models;
using System.Windows.Input;

namespace APP.Components;

public partial class RouteCardView : ContentView
{
    public RouteCardView()
    {
        InitializeComponent();
    }

    public static readonly BindableProperty TitleProperty =
        BindableProperty.Create(nameof(Title), typeof(string), typeof(RouteCardView));

    public static readonly BindableProperty DistanceMetersProperty =
        BindableProperty.Create(nameof(DistanceMeters), typeof(double), typeof(RouteCardView), 0.0,
            propertyChanged: (b, o, n) => ((RouteCardView)b).OnNotifyLabelsChanged());

    public static readonly BindableProperty ElevationGainMetersProperty =
        BindableProperty.Create(nameof(ElevationGainMeters), typeof(double), typeof(RouteCardView), 0.0,
            propertyChanged: (b, o, n) => ((RouteCardView)b).OnNotifyLabelsChanged());

    public static readonly BindableProperty DifficultyProperty =
        BindableProperty.Create(nameof(Difficulty), typeof(RouteDifficulty), typeof(RouteCardView), RouteDifficulty.VeryEasy,
            propertyChanged: (b, o, n) => ((RouteCardView)b).OnNotifyLabelsChanged());

    public static readonly BindableProperty ThumbnailSourceProperty =
        BindableProperty.Create(nameof(ThumbnailSource), typeof(ImageSource), typeof(RouteCardView));

    public static readonly BindableProperty IsFavoriteProperty =
        BindableProperty.Create(nameof(IsFavorite), typeof(bool), typeof(RouteCardView));

    public static readonly BindableProperty ToggleFavoriteCommandProperty =
        BindableProperty.Create(nameof(ToggleFavoriteCommand), typeof(ICommand), typeof(RouteCardView));

    public static readonly BindableProperty TapCommandProperty =
        BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(RouteCardView));
    public static readonly BindableProperty ItemProperty =
        BindableProperty.Create(nameof(Item), typeof(object), typeof(RouteCardView));
    public static readonly BindableProperty OptionsCommandProperty =
        BindableProperty.Create(nameof(OptionsCommand), typeof(ICommand), typeof(RouteCardView));

    public string Title
    {
        get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value);
    }
    public double DistanceMeters
    {
        get => (double)GetValue(DistanceMetersProperty); set => SetValue(DistanceMetersProperty, value);
    }
    public double ElevationGainMeters
    {
        get => (double)GetValue(ElevationGainMetersProperty); set => SetValue(ElevationGainMetersProperty, value);
    }
    public RouteDifficulty Difficulty
    {
        get => (RouteDifficulty)GetValue(DifficultyProperty); set => SetValue(DifficultyProperty, value);
    }
    public ImageSource ThumbnailSource
    {
        get => (ImageSource)GetValue(ThumbnailSourceProperty); set => SetValue(ThumbnailSourceProperty, value);
    }
    public bool IsFavorite
    {
        get => (bool)GetValue(IsFavoriteProperty); set => SetValue(IsFavoriteProperty, value);
    }
    public ICommand ToggleFavoriteCommand
    {
        get => (ICommand)GetValue(ToggleFavoriteCommandProperty); set => SetValue(ToggleFavoriteCommandProperty, value);
    }
    public ICommand TapCommand
    {
        get => (ICommand)GetValue(TapCommandProperty); set => SetValue(TapCommandProperty, value);
    }
    public object Item
    {
        get => GetValue(ItemProperty); set => SetValue(ItemProperty, value);
    }
    public ICommand OptionsCommand
    {
        get => (ICommand)GetValue(OptionsCommandProperty); set => SetValue(OptionsCommandProperty, value);
    }

    public string DistanceLabel => $"⟷ {DistanceMeters / 1000:F2} km";
    public string ElevationLabel => $"⛰ {ElevationGainMeters:F0} m";
    public bool IsProDifficulty => Difficulty == RouteDifficulty.Pro;

    private void OnNotifyLabelsChanged()
    {
        OnPropertyChanged(nameof(DistanceLabel));
        OnPropertyChanged(nameof(ElevationLabel));
        OnPropertyChanged(nameof(IsProDifficulty));
    }
}