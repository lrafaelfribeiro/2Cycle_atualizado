using System.Globalization;

namespace APP.Converters;

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
