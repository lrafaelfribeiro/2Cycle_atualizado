namespace APP.Components
{
    public partial class PauseControlRowView : ContentView
    {
        public static readonly BindableProperty IconProperty = BindableProperty.Create(
            nameof(Icon), typeof(string), typeof(PauseControlRowView), string.Empty);

        public static readonly BindableProperty TitleProperty = BindableProperty.Create(
            nameof(Title), typeof(string), typeof(PauseControlRowView), string.Empty);

        public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
            nameof(Description), typeof(string), typeof(PauseControlRowView), string.Empty);

        public static readonly BindableProperty IsToggledProperty = BindableProperty.Create(
            nameof(IsToggled), typeof(bool), typeof(PauseControlRowView), false,
            BindingMode.TwoWay); // TwoWay é essencial aqui — sem isto, o AutoPauseEnabled do ViewModel nunca recebe o toque do utilizador no switch

        public static readonly BindableProperty SwitchOnColorProperty = BindableProperty.Create(
            nameof(SwitchOnColor), typeof(Color), typeof(PauseControlRowView), Color.FromArgb("#A8E900"));

        public static readonly BindableProperty SwitchThumbColorProperty = BindableProperty.Create(
            nameof(SwitchThumbColor), typeof(Color), typeof(PauseControlRowView), Colors.White);

        public string Icon { get => (string)GetValue(IconProperty); set => SetValue(IconProperty, value); }
        public string Title { get => (string)GetValue(TitleProperty); set => SetValue(TitleProperty, value); }
        public string Description { get => (string)GetValue(DescriptionProperty); set => SetValue(DescriptionProperty, value); }
        public bool IsToggled { get => (bool)GetValue(IsToggledProperty); set => SetValue(IsToggledProperty, value); }
        public Color SwitchOnColor { get => (Color)GetValue(SwitchOnColorProperty); set => SetValue(SwitchOnColorProperty, value); }
        public Color SwitchThumbColor { get => (Color)GetValue(SwitchThumbColorProperty); set => SetValue(SwitchThumbColorProperty, value); }

        public PauseControlRowView()
        {
            InitializeComponent();
        }
    }
}