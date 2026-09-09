namespace APP.Components
{
    public partial class OptionRowView : ContentView
    {
        public static readonly BindableProperty IconSourceProperty = BindableProperty.Create(
            nameof(IconSource), typeof(string), typeof(OptionRowView), string.Empty);

        public static readonly BindableProperty TextProperty = BindableProperty.Create(
            nameof(Text), typeof(string), typeof(OptionRowView), string.Empty);

        public static readonly BindableProperty TextColorProperty = BindableProperty.Create(
            nameof(TextColor), typeof(Color), typeof(OptionRowView), Color.FromArgb("#EAEAEA"));

        public string IconSource
        {
            get => (string)GetValue(IconSourceProperty);
            set => SetValue(IconSourceProperty, value);
        }

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public Color TextColor
        {
            get => (Color)GetValue(TextColorProperty);
            set => SetValue(TextColorProperty, value);
        }

        public event EventHandler? Tapped;

        public OptionRowView()
        {
            InitializeComponent();
        }

        private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);
    }
}