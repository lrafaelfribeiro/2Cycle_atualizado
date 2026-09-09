using System.Globalization;

namespace APP.Converters
{
    public class BoolToTabColorConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool boundValue = value is bool b && b;
            bool invert = string.Equals(parameter as string, "Invert", StringComparison.OrdinalIgnoreCase);
            bool isActive = invert ? !boundValue : boundValue;

            string key = isActive ? "TextPrimary" : "TextSecondary";
            return Application.Current!.Resources.TryGetValue(key, out var color) && color is Color c
                ? c
                : Colors.White;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}