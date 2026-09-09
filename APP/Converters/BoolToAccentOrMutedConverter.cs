using System.Globalization;

namespace APP.Converters
{
    public class BoolToAccentOrMutedConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isValid = value is true;
            return isValid
                ? Application.Current!.Resources["Primary"]
                : Application.Current!.Resources["TextSecondary"];
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
