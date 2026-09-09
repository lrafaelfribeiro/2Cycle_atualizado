namespace APP.Components
{
    public partial class MetricCardView : ContentView
    {
        public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
            nameof(IconSource), typeof(string), typeof(MetricCardView), string.Empty);

        public static readonly BindableProperty IconColorProperty = BindableProperty.Create(
            nameof(IconColor), typeof(Color), typeof(MetricCardView), Color.FromArgb("#A8E900"));

        public static readonly BindableProperty LabelProperty = BindableProperty.Create(
            nameof(Label), typeof(string), typeof(MetricCardView), string.Empty);

        public static readonly BindableProperty ValueProperty = BindableProperty.Create(
            nameof(Value), typeof(string), typeof(MetricCardView), string.Empty);

        public static readonly BindableProperty UnitProperty = BindableProperty.Create(
            nameof(Unit), typeof(string), typeof(MetricCardView), string.Empty);

        public string IconSource { get => (string)GetValue(IconSourceProperty); set => SetValue(IconSourceProperty, value); }
        public Color IconColor { get => (Color)GetValue(IconColorProperty); set => SetValue(IconColorProperty, value); }
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
        public string Value { get => (string)GetValue(ValueProperty); set => SetValue(ValueProperty, value); }
        public string Unit { get => (string)GetValue(UnitProperty); set => SetValue(UnitProperty, value); }

        public MetricCardView()
        {
            InitializeComponent();
        }
    }
}