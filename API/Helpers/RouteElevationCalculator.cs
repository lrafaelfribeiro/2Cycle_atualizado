using API.DTOs.Routes;
using API.Models;
using API.Services.OpenRouteService;

namespace API.Services.Routes
{
    /// <summary>
    /// Cálculo de perfil de elevação e distância acumulada, partilhado entre
    /// RouteSuggestionService (preview genérico) e SavedRouteService (detalhe de rota guardada).
    /// Nunca persistido — calculado on-the-fly, tal como o IMC (ver CLAUDE.md).
    /// </summary>
    internal static class RouteElevationCalculator
    {
        private const double EarthRadiusMeters = 6371000;

        public static ElevationSummary BuildSummary(List<RoutePoint> orderedPoints)
        {
            return BuildSummaryCore(
                orderedPoints,
                getLatitude: p => p.Latitude,
                getLongitude: p => p.Longitude,
                getAltitudeRaw: p => p.AltitudeMeters);
        }

        /// <summary>
        /// Sobrecarga para pontos ainda não persistidos (resposta bruta da ORS) — usada
        /// no preview de uma rota pendente, antes de existir SuggestedRoute/RoutePoint na BD.
        /// </summary>
        public static ElevationSummary BuildSummary(List<ORSRoutePoint> orderedPoints)
        {
            return BuildSummaryCore(
                orderedPoints,
                getLatitude: p => p.Latitude,
                getLongitude: p => p.Longitude,
                getAltitudeRaw: p => p.AltitudeMeters);
        }

        /// <summary>
        /// Núcleo genérico do cálculo — partilhado pelas duas sobrecargas acima para não
        /// duplicar a lógica de gain/loss/net entre RoutePoint (BD) e ORSRoutePoint (preview).
        /// getAltitudeRaw devolve nullable porque a ORS pode, em casos raros, não devolver
        /// altitude para um ponto específico; usa-se "carry-forward" (mantém a última altitude
        /// conhecida) em vez de assumir 0 — coalescer para 0 criaria um vale/pico falso ao
        /// nível do mar sempre que houvesse um gap no meio de uma subida real.
        /// </summary>
        private static ElevationSummary BuildSummaryCore<T>(
            List<T> orderedPoints,
            Func<T, double> getLatitude,
            Func<T, double> getLongitude,
            Func<T, double?> getAltitudeRaw)
        {
            var profile = new List<ElevationPointResponse>(orderedPoints.Count);
            double cumulativeDistance = 0;
            double gain = 0;
            double loss = 0;
            double lastKnownAltitude = 0;
            double previousResolvedAltitude = 0;

            for (int i = 0; i < orderedPoints.Count; i++)
            {
                var point = orderedPoints[i];
                var rawAltitude = getAltitudeRaw(point);
                if (rawAltitude.HasValue)
                {
                    lastKnownAltitude = rawAltitude.Value;
                }
                double resolvedAltitude = lastKnownAltitude;

                if (i > 0)
                {
                    var prev = orderedPoints[i - 1];

                    cumulativeDistance += HaversineDistanceMeters(
                        getLatitude(prev), getLongitude(prev),
                        getLatitude(point), getLongitude(point));

                    var delta = resolvedAltitude - previousResolvedAltitude;
                    if (delta > 0) gain += delta;
                    else loss += Math.Abs(delta);
                }

                profile.Add(new ElevationPointResponse(cumulativeDistance, resolvedAltitude));
                previousResolvedAltitude = resolvedAltitude;
            }

            double net = orderedPoints.Count > 0
                ? previousResolvedAltitude - (getAltitudeRaw(orderedPoints[0]) ?? previousResolvedAltitude)
                : 0;

            return new ElevationSummary(gain, loss, net, profile);
        }

        /// <summary>
        /// Ganho de elevação calculado a partir da resposta bruta do ORS (usado só na criação da rota,
        /// antes de existirem RoutePoint persistidos).
        /// </summary>
        public static double CalculateElevationGainFromOrs(List<ORSRoutePoint> points)
        {
            double gain = 0;

            for (int i = 1; i < points.Count; i++)
            {
                double? previous = points[i - 1].AltitudeMeters;
                double? current = points[i].AltitudeMeters;

                if (previous.HasValue && current.HasValue && current.Value > previous.Value)
                {
                    gain += current.Value - previous.Value;
                }
            }

            return gain;
        }

        public static double HaversineDistanceMeters(double lat1, double lon1, double lat2, double lon2)
        {
            var dLat = double.DegreesToRadians(lat2 - lat1);
            var dLon = double.DegreesToRadians(lon2 - lon1);

            var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                    Math.Cos(double.DegreesToRadians(lat1)) * Math.Cos(double.DegreesToRadians(lat2)) *
                    Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

            var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
            return EarthRadiusMeters * c;
        }
    }
}