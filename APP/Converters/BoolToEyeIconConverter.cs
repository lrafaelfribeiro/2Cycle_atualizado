using System.Globalization;

namespace APP.Converters
{
    public class BoolToEyeIconConverter : IValueConverter
    {
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            bool isHidden = value is true;
            return isHidden ? "eye.svg" : "eye_closed.svg";
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();
    }
}
