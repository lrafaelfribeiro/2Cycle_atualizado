using API.Models;

namespace API.Services.Routes
{
    public static class RouteDifficultyClassifier
    {
        private const double MetersOfGainPerEquivalentKm = 100;

        // 5 thresholds definem 6 níveis (o último, acima do maior threshold, é "Pro")
        private static readonly double[] EquivalentDistanceThresholdsKm = { 12, 25, 45, 80, 180 };
        private static readonly double[] AverageGradientThresholds = { 15, 30, 50, 80, 100 };

        public static RouteDifficulty Classify(double elevationGainMeters, double distanceMeters)
        {
            if (distanceMeters <= 0)
            {
                return RouteDifficulty.VeryEasy;
            }

            RouteDifficulty distanceDifficulty = ClassifyByEquivalentDistance(elevationGainMeters, distanceMeters);
            RouteDifficulty gradientDifficulty = ClassifyByAverageGradient(elevationGainMeters, distanceMeters);

            return (RouteDifficulty)Math.Max((int)distanceDifficulty, (int)gradientDifficulty);
        }

        private static RouteDifficulty ClassifyByEquivalentDistance(double elevationGainMeters, double distanceMeters)
        {
            double distanceKm = distanceMeters / 1000;
            double equivalentDistanceKm = distanceKm + (elevationGainMeters / MetersOfGainPerEquivalentKm);
            return LevelFromThresholds(equivalentDistanceKm, EquivalentDistanceThresholdsKm);
        }

        private static RouteDifficulty ClassifyByAverageGradient(double elevationGainMeters, double distanceMeters)
        {
            double distanceKm = distanceMeters / 1000;
            double averageGradientPerKm = elevationGainMeters / distanceKm;
            return LevelFromThresholds(averageGradientPerKm, AverageGradientThresholds);
        }

        private static RouteDifficulty LevelFromThresholds(double value, double[] thresholds)
        {
            for (int i = 0; i < thresholds.Length; i++)
            {
                if (value <= thresholds[i])
                {
                    return (RouteDifficulty)i;
                }
            }

            return RouteDifficulty.Pro;
        }
    }
}