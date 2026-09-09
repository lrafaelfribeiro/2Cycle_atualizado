using APP.Models.Local;

namespace APP.Core
{
    public record SpeedZoneResult(string Label, string RangeLabel, TimeSpan Duration, double Percentage);

    public static class SpeedZoneCalculator
    {
        private static readonly (string Label, double MinKmh, double MaxKmh)[] Zones =
        {
            ("Zona 1", 0, 15),
            ("Zona 2", 15, 20),
            ("Zona 3", 20, 25),
            ("Zona 4", 25, 30),
            ("Zona 5", 30, double.MaxValue),
        };

        public static List<SpeedZoneResult> Calculate(List<(List<LocalTrackPoint> Points, DateTime SegmentStart)> segments)
        {
            var secondsPerZone = new double[Zones.Length];
            double totalMovingSeconds = 0;

            foreach (var (points, _) in segments)
            {
                for (int i = 1; i < points.Count; i++)
                {
                    var prev = points[i - 1];
                    var curr = points[i];

                    double deltaSeconds = (curr.RecordedAt - prev.RecordedAt).TotalSeconds;
                    if (deltaSeconds <= 0) continue;

                    double deltaMeters = GeoMath.HaversineDistanceMeters(prev.Latitude, prev.Longitude, curr.Latitude, curr.Longitude);
                    double speedKmh = (deltaMeters / deltaSeconds) * 3.6;

                    // filtro de ruído
                    if (speedKmh > 180) continue;

                    int zoneIndex = Array.FindLastIndex(Zones, z => speedKmh >= z.MinKmh);
                    if (zoneIndex < 0) zoneIndex = 0;

                    secondsPerZone[zoneIndex] += deltaSeconds;
                    totalMovingSeconds += deltaSeconds;
                }
            }

            var results = new List<SpeedZoneResult>();
            for (int i = 0; i < Zones.Length; i++)
            {
                double percentage = totalMovingSeconds > 0 ? (secondsPerZone[i] / totalMovingSeconds) * 100 : 0;
                string rangeLabel = Zones[i].MaxKmh == double.MaxValue
                    ? $"> {Zones[i].MinKmh:F0} km/h"
                    : $"{Zones[i].MinKmh:F0} - {Zones[i].MaxKmh:F0} km/h";

                results.Add(new SpeedZoneResult(Zones[i].Label, rangeLabel, TimeSpan.FromSeconds(secondsPerZone[i]), percentage));
            }

            results.Reverse(); 
            return results;
        }
    }
}
