using System.Globalization;
using static APP.Models.Routes.RouteListItem;

namespace APP.Converters
{
    public class RouteStateVisibilityConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not RouteListItemState state || parameter is not string targetStateName)
            {
                return false;
            }

            return Enum.TryParse<RouteListItemState>(targetStateName, out var targetState)
                && state == targetState;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}