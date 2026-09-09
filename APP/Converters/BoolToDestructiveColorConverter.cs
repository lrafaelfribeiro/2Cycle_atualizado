using System.Globalization;

namespace APP.Converters;

// true (destructive) -> TextDanger, false -> cor de texto normal do tema.
// Existe separado do BoolToAccentOrMutedConverter porque a semântica é diferente:
// aqui o bool representa "é uma ação perigosa", não "está selecionado/ativo".
public sealed class BoolToDestructiveColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        bool isDestructive = value is true;

        return isDestructive
            ? Application.Current!.Resources["TextDanger"]
            : Application.Current!.Resources["TextPrimary"]; 
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
