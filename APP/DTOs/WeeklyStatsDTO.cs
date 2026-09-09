using System;
using System.Collections.Generic;
using System.Text;

namespace APP.Models
{
    public sealed record WeeklyStatsDTO(double DistanceKm, TimeSpan MovingTime, double ElevationGainMeters);
}
