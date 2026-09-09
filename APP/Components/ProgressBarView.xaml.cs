namespace APP.Components;

public partial class ProgressBarView : ContentView
{
    public static readonly BindableProperty ValueProperty =
        BindableProperty.Create(
            nameof(Value),
            typeof(double),
            typeof(ProgressBarView),
            0.0,
            propertyChanged: OnProgressChanged);

    public static readonly BindableProperty MaximumProperty =
        BindableProperty.Create(
            nameof(Maximum),
            typeof(double),
            typeof(ProgressBarView),
            100.0,
            propertyChanged: OnProgressChanged);

    public double Value
    {
        get => (double)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public double Maximum
    {
        get => (double)GetValue(MaximumProperty);
        set => SetValue(MaximumProperty, value);
    }

    public ProgressBarView()
    {
        InitializeComponent();

        SizeChanged += (_, _) => UpdateProgress();
    }

    private static void OnProgressChanged(
        BindableObject bindable,
        object oldValue,
        object newValue)
    {
        ((ProgressBarView)bindable).UpdateProgress();
    }

    private void UpdateProgress()
    {
        if (Maximum <= 0)
        {
            PercentageLabel.Text = "0%";
            return;
        }

        double progress = Math.Clamp(Value / Maximum, 0, 1);

        ProgressContainer.ColumnDefinitions.Clear();

        ProgressContainer.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(progress, GridUnitType.Star)
            });

        ProgressContainer.ColumnDefinitions.Add(
            new ColumnDefinition
            {
                Width = new GridLength(1 - progress, GridUnitType.Star)
            });

        PercentageLabel.Text = $"{Math.Round(progress * 100)}%";
    }
}
