using System.Windows.Input;

namespace APP.Components
{
    public partial class CardShell : ContentView
    {
        public const double ThumbnailSize = 90;
        public const double ColumnSpacing = 12;

        public static readonly BindableProperty LeftContentProperty =
            BindableProperty.Create(nameof(LeftContent), typeof(View), typeof(CardShell));

        public static readonly BindableProperty DetailsContentProperty =
            BindableProperty.Create(nameof(DetailsContent), typeof(View), typeof(CardShell));

        public static readonly BindableProperty TrailingContentProperty =
            BindableProperty.Create(nameof(TrailingContent), typeof(View), typeof(CardShell));

        public static readonly BindableProperty TapCommandProperty =
            BindableProperty.Create(nameof(TapCommand), typeof(ICommand), typeof(CardShell));

        public static readonly BindableProperty TapCommandParameterProperty =
            BindableProperty.Create(nameof(TapCommandParameter), typeof(object), typeof(CardShell));

        public View LeftContent
        {
            get => (View)GetValue(LeftContentProperty);
            set => SetValue(LeftContentProperty, value);
        }

        public View DetailsContent
        {
            get => (View)GetValue(DetailsContentProperty);
            set => SetValue(DetailsContentProperty, value);
        }

        public View TrailingContent
        {
            get => (View)GetValue(TrailingContentProperty);
            set => SetValue(TrailingContentProperty, value);
        }

        public ICommand TapCommand
        {
            get => (ICommand)GetValue(TapCommandProperty);
            set => SetValue(TapCommandProperty, value);
        }

        public object TapCommandParameter
        {
            get => GetValue(TapCommandParameterProperty);
            set => SetValue(TapCommandParameterProperty, value);
        }

        public CardShell()
        {
            InitializeComponent();
        }
    }
}