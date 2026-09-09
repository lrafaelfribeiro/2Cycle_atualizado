namespace API.Helpers
{
    /// <summary>
    /// Simplificação de polilinhas via Douglas-Peucker. Usado para reduzir o número
    /// de RoutePoint persistidos sem perder a forma visual da rota no mapa.
    /// Não deve ser aplicado antes do cálculo de elevação (RouteElevationCalculator) —
    /// simplificar a geometria perde picos/vales reais que o gráfico de elevação precisa.
    /// </summary>
    public static class RoutePolylineSimplifier
    {
        // Tolerância em graus decimais. ~0.00005 ≈ 5 metros — suficiente para não
        // se notar visualmente num mapa, mas já corta bastante ruído GPS/ORS.
        private const double DefaultToleranceDegrees = 0.00005;

        public static List<T> Simplify<T>(
            IReadOnlyList<T> points,
            Func<T, double> getLatitude,
            Func<T, double> getLongitude,
            double toleranceDegrees = DefaultToleranceDegrees)
        {
            if (points.Count <= 2)
            {
                return points.ToList();
            }

            var keepFlags = new bool[points.Count];
            keepFlags[0] = true;
            keepFlags[^1] = true;

            SimplifySection(points, getLatitude, getLongitude, 0, points.Count - 1, toleranceDegrees, keepFlags);

            return points.Where((_, i) => keepFlags[i]).ToList();
        }

        private static void SimplifySection<T>(
            IReadOnlyList<T> points,
            Func<T, double> getLatitude,
            Func<T, double> getLongitude,
            int startIndex, int endIndex,
            double toleranceDegrees,
            bool[] keepFlags)
        {
            if (endIndex <= startIndex + 1)
            {
                return;
            }

            var (farthestIndex, farthestDistance) = FindFarthestPoint(
                points, getLatitude, getLongitude, startIndex, endIndex);

            if (farthestDistance <= toleranceDegrees)
            {
                return; // todos os pontos intermédios estão "perto" da reta — descarta-os
            }

            keepFlags[farthestIndex] = true;

            SimplifySection(points, getLatitude, getLongitude, startIndex, farthestIndex, toleranceDegrees, keepFlags);
            SimplifySection(points, getLatitude, getLongitude, farthestIndex, endIndex, toleranceDegrees, keepFlags);
        }

        private static (int Index, double Distance) FindFarthestPoint<T>(
            IReadOnlyList<T> points,
            Func<T, double> getLatitude,
            Func<T, double> getLongitude,
            int startIndex, int endIndex)
        {
            int farthestIndex = startIndex;
            double farthestDistance = 0;

            for (int i = startIndex + 1; i < endIndex; i++)
            {
                double distance = PerpendicularDistance(
                    getLatitude(points[i]), getLongitude(points[i]),
                    getLatitude(points[startIndex]), getLongitude(points[startIndex]),
                    getLatitude(points[endIndex]), getLongitude(points[endIndex]));

                if (distance > farthestDistance)
                {
                    farthestDistance = distance;
                    farthestIndex = i;
                }
            }

            return (farthestIndex, farthestDistance);
        }

        // Distância perpendicular do ponto (px,py) à reta definida por (ax,ay)-(bx,by).
        // Aproximação planar em graus — suficiente para simplificação visual; não é
        // geodesicamente exata, mas o erro é desprezável à escala de uma rota de ciclismo.
        private static double PerpendicularDistance(
            double px, double py, double ax, double ay, double bx, double by)
        {
            double dx = bx - ax;
            double dy = by - ay;

            if (dx == 0 && dy == 0)
            {
                return Math.Sqrt(Math.Pow(px - ax, 2) + Math.Pow(py - ay, 2));
            }

            double t = ((px - ax) * dx + (py - ay) * dy) / (dx * dx + dy * dy);
            t = Math.Clamp(t, 0, 1);

            double projX = ax + t * dx;
            double projY = ay + t * dy;

            return Math.Sqrt(Math.Pow(px - projX, 2) + Math.Pow(py - projY, 2));
        }
    }
}