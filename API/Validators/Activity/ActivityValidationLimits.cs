namespace API.Validators
{
    public static class ActivityValidationLimits
    {
        public const int MaxSegments = 20;
        public const int MaxPointsPerSegment = 5_000;
        public const int MaxTotalPoints = 20_000;

        public const double MaxDistanceMeters = 1_000_000;
        public const double MaxAverageSpeedKmh = 200;
        public const double MaxSpeedKmh = 300;
        public const double MaxElevationGainMeters = 50_000;

        public const int MaxTotalTimeSeconds = 24 * 60 * 60;
    }
}