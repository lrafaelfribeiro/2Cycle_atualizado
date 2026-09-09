using System.Globalization;

namespace APP.Converters
{
    /// <summary>
    /// Converte um bool num ImageSource, escolhendo entre dois ficheiros passados no ConverterParameter
    /// no formato "quandoTrue.svg|quandoFalse.svg". Uso: favoritos, toggles com ícone, etc.
    /// Devolve ImageSource diretamente (não string) para não depender do TypeConverter da propriedade de destino.
    /// </summary>
    public class BoolToIconSourceConverter : IValueConverter
    {
        private const char IconSeparator = '|';

        public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not bool isTrue || parameter is not string iconPair)
            {
                return null;
            }

            var icons = iconPair.Split(IconSeparator);
            if (icons.Length != 2)
            {
                throw new ArgumentException(
                    $"ConverterParameter tem de ser \"iconeTrue{IconSeparator}iconeFalse\", recebeu \"{iconPair}\".");
            }

            var chosenIcon = isTrue ? icons[0] : icons[1];
            return ImageSource.FromFile(chosenIcon);
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException($"{nameof(BoolToIconSourceConverter)} só suporta conversão numa direção.");
    }
}