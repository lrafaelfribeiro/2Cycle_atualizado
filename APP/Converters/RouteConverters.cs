using System.Globalization;
using static Android.Provider.CalendarContract;

namespace APP.Converters
{
    /// <summary>
    /// Fundo do botão de modo (Ponto a ponto / Ida e volta).
    /// value = IsRoundTrip (bool). parameter = "true" ou "false" (a que modo este botão corresponde).
    /// Fica verde se o modo atual bater com o parâmetro, cinzento caso contrário.
    /// </summary>
    public class ModeButtonBgConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isRoundTrip = value is bool b && b;
            bool thisButtonIsRoundTrip = parameter?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

            return isRoundTrip == thisButtonIsRoundTrip
                ? Color.FromArgb("#A6E22E")
                : Color.FromArgb("#2A2A2E");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Cor do texto do botão de modo — segue a mesma lógica do fundo, mas invertida
    /// (texto escuro sobre fundo verde ativo; texto claro sobre fundo cinzento inativo).
    /// </summary>
    public class ModeButtonTextConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isRoundTrip = value is bool b && b;
            bool thisButtonIsRoundTrip = parameter?.ToString()?.Equals("true", StringComparison.OrdinalIgnoreCase) ?? false;

            return isRoundTrip == thisButtonIsRoundTrip
                ? Color.FromArgb("#0D0D0F")
                : Color.FromArgb("#9A9A9E");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

    /// <summary>
    /// Cor do ícone de "trocar origem/destino" — branco quando ativo (CanSwap == true),
    /// cinzento apagado quando desativado (só um ponto definido, ainda não há o que trocar).
    /// </summary>
    public class SwapEnabledColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool canSwap = value is bool b && b;
            return canSwap ? Microsoft.Maui.Graphics.Colors.White : Color.FromArgb("#4A4A4E");
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }

}
