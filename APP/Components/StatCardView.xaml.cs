namespace APP.Components;

public partial class StatCardView : ContentView
{
    public static readonly BindableProperty ImageProperty = BindableProperty.Create(
            nameof(Image), typeof(string), typeof(StatCardView), string.Empty);

    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
            nameof(Label), typeof(string), typeof(StatCardView), string.Empty);

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(StatCardView), string.Empty);

    public string Image
    {
        get => (string)GetValue(ImageProperty);
        set => SetValue(ImageProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public StatCardView()
	{
		InitializeComponent();
	}
}