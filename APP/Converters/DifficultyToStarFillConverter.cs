using APP.Models;
using System.Globalization;

namespace APP.Converters
{
    /// <summary>
    /// Converte RouteDifficulty + posição da estrela (ConverterParameter, "1".."5")
    /// em star_filled.svg ou star_outline.svg — para desenhar o rating em 5 estrelas.
    /// </summary>
    public class DifficultyToStarFillConverter : IValueConverter
    {
        private const string FilledIcon = "star_filled.svg";
        private const string OutlineIcon = "star_outline.svg";

        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            if (value is not RouteDifficulty difficulty || parameter is not string starPositionText)
            {
                return OutlineIcon;
            }

            int starPosition = int.Parse(starPositionText);
            int filledStars = DifficultyToStarCount(difficulty);

            return starPosition <= filledStars ? FilledIcon : OutlineIcon;
        }

        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
            => throw new NotSupportedException();

        private static int DifficultyToStarCount(RouteDifficulty difficulty) => difficulty switch
        {
            RouteDifficulty.VeryEasy => 1,
            RouteDifficulty.Easy => 2,
            RouteDifficulty.Moderate => 3,
            RouteDifficulty.Hard => 4,
            RouteDifficulty.VeryHard => 5,
            RouteDifficulty.Pro => 5,
            _ => 1
        };
    }
}