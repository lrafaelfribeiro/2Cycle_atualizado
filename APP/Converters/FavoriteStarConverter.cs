using System.Globalization;

namespace APP.Converters
{
    public class FavoriteStarConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
            => value is bool isFavorite && isFavorite ? "★" : "☆";

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}