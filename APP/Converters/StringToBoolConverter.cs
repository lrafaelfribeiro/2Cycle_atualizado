using System.Globalization;

namespace APP.Converters
{
    /// <summary>
    /// Converte string -> bool: true se a string não for nula/vazia/whitespace.
    /// Usado tipicamente para IsVisible bindings condicionais a um Label ter conteúdo.
    /// </summary>
    public class StringToBoolConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            return !string.IsNullOrWhiteSpace(value as string);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            throw new NotSupportedException("StringToBoolConverter é one-way (não suporta ConvertBack).");
        }
    }
}