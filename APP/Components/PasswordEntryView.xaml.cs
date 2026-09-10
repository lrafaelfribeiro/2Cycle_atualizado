using System.Windows.Input;

namespace APP.Components
{
    public partial class PasswordEntryView : ContentView
    {
        public static readonly BindableProperty TextProperty =
            BindableProperty.Create(nameof(Text), typeof(string), typeof(PasswordEntryView), string.Empty, BindingMode.TwoWay);

        public static readonly BindableProperty PlaceholderProperty =
            BindableProperty.Create(nameof(Placeholder), typeof(string), typeof(PasswordEntryView), "Password");

        public static readonly BindableProperty IsPasswordHiddenProperty =
            BindableProperty.Create(nameof(IsPasswordHidden), typeof(bool), typeof(PasswordEntryView), true);

        public static readonly BindableProperty ReturnTypeProperty =
            BindableProperty.Create(nameof(ReturnType), typeof(ReturnType), typeof(PasswordEntryView), Microsoft.Maui.ReturnType.Done);

        public static readonly BindableProperty ReturnCommandProperty =
            BindableProperty.Create(nameof(ReturnCommand), typeof(ICommand), typeof(PasswordEntryView));

        public string Text
        {
            get => (string)GetValue(TextProperty);
            set => SetValue(TextProperty, value);
        }

        public string Placeholder
        {
            get => (string)GetValue(PlaceholderProperty);
            set => SetValue(PlaceholderProperty, value);
        }

        public bool IsPasswordHidden
        {
            get => (bool)GetValue(IsPasswordHiddenProperty);
            private set => SetValue(IsPasswordHiddenProperty, value);
        }

        public ReturnType ReturnType
        {
            get => (ReturnType)GetValue(ReturnTypeProperty);
            set => SetValue(ReturnTypeProperty, value);
        }

        public ICommand? ReturnCommand
        {
            get => (ICommand?)GetValue(ReturnCommandProperty);
            set => SetValue(ReturnCommandProperty, value);
        }
        public event EventHandler? Completed;
        public PasswordEntryView()
        {
            InitializeComponent();
            PasswordEntry.Completed += (s, e) => Completed?.Invoke(this, e);
        }

        private void OnToggleClicked(object? sender, EventArgs e) =>
            IsPasswordHidden = !IsPasswordHidden;

        /// <summary>Dá foco à Entry interna — usar no chaining de campos (ex: Email → Password).</summary>
        public void FocusEntry() => PasswordEntry.Focus();
    }
}