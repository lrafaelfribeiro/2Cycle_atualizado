using APP.DTOs.Routes;
using APP.DTOs.Routes.Responses;

namespace APP.Controls
{
    public class ElevationChartDrawable : IDrawable
    {
        public List<ElevationPointResponse> Profile { get; set; } = new();

        private const float LeftMargin = 42;   // espaço para labels do eixo Y ("150 m")
        private const float BottomMargin = 24; // espaço para labels do eixo X ("3.1 km")
        private const float TopMargin = 8;
        private const float RightMargin = 8;

        private const int YAxisTicks = 4; // 0, 1/4, 2/4, 3/4, topo -> 5 labels
        private const int XAxisTicks = 4;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Profile.Count < 2) return;

            var chartArea = new RectF(
                dirtyRect.Left + LeftMargin,
                dirtyRect.Top + TopMargin,
                dirtyRect.Width - LeftMargin - RightMargin,
                dirtyRect.Height - TopMargin - BottomMargin);

            double maxDistance = Profile[^1].DistanceMeters;

            // Sem isto, rotas com distância acumulada 0 (pontos duplicados, dados
            // malformados) geram NaN/Infinity em ScaleX, que crasha o renderer
            // nativo (SkiaSharp) ao tentar desenhar coordenadas inválidas.
            if (maxDistance <= 0) return;

            double minAltitude = Profile.Min(p => p.AltitudeMeters);
            double maxAltitude = Profile.Max(p => p.AltitudeMeters);
            double altitudeRange = Math.Max(maxAltitude - minAltitude, 1);

            float ScaleX(double d) => chartArea.Left + (float)(d / maxDistance * chartArea.Width);
            float ScaleY(double alt) => chartArea.Bottom - (float)((alt - minAltitude) / altitudeRange * chartArea.Height);

            DrawYAxis(canvas, chartArea, minAltitude, maxAltitude);
            DrawXAxis(canvas, chartArea, maxDistance);
            DrawCurve(canvas, chartArea, ScaleX, ScaleY);
        }

        private void DrawCurve(ICanvas canvas, RectF chartArea, Func<double, float> scaleX, Func<double, float> scaleY)
        {
            var path = new PathF();
            path.MoveTo(scaleX(0), scaleY(Profile[0].AltitudeMeters));
            foreach (var point in Profile.Skip(1))
                path.LineTo(scaleX(point.DistanceMeters), scaleY(point.AltitudeMeters));

            var fillPath = new PathF(path);
            fillPath.LineTo(scaleX(Profile[^1].DistanceMeters), chartArea.Bottom);
            fillPath.LineTo(scaleX(0), chartArea.Bottom);
            fillPath.Close();

            canvas.FillColor = Color.FromArgb("#33A6E22E");
            canvas.FillPath(fillPath);

            canvas.StrokeColor = Color.FromArgb("#A6E22E");
            canvas.StrokeSize = 3;
            canvas.DrawPath(path);
        }

        private static void DrawYAxis(ICanvas canvas, RectF chartArea, double minAltitude, double maxAltitude)
        {
            canvas.FontSize = 11;
            canvas.FontColor = Color.FromArgb("#8A8A8E");

            for (int i = 0; i <= YAxisTicks; i++)
            {
                double fraction = i / (double)YAxisTicks;
                double altitude = minAltitude + fraction * (maxAltitude - minAltitude);
                float y = chartArea.Bottom - (float)(fraction * chartArea.Height);

                // Linha de grelha horizontal, subtil
                canvas.StrokeColor = Color.FromArgb("#1F1F22");
                canvas.StrokeSize = 1;
                canvas.DrawLine(chartArea.Left, y, chartArea.Right, y);

                // Label alinhado à direita, encostado à linha
                var labelRect = new RectF(0, y - 7, LeftMargin - 6, 14);
                canvas.DrawString($"{altitude:F0} m", labelRect, HorizontalAlignment.Right, VerticalAlignment.Center);
            }
        }

        private static void DrawXAxis(ICanvas canvas, RectF chartArea, double maxDistanceMeters)
        {
            canvas.FontSize = 11;
            canvas.FontColor = Color.FromArgb("#8A8A8E");

            double maxDistanceKm = maxDistanceMeters / 1000.0;

            for (int i = 0; i <= XAxisTicks; i++)
            {
                double fraction = i / (double)XAxisTicks;
                double distanceKm = fraction * maxDistanceKm;
                float x = chartArea.Left + (float)(fraction * chartArea.Width);

                RectF labelRect;
                HorizontalAlignment alignment;

                if (i == 0)
                {
                    // cresce para a direita a partir do início do gráfico
                    // (não invade a zona da label do eixo Y)
                    labelRect = new RectF(x, chartArea.Bottom + 4, 48, 16);
                    alignment = HorizontalAlignment.Left;
                }
                else if (i == XAxisTicks)
                {
                    // termina exatamente no fim da área do gráfico
                    // (não sai para fora do dirtyRect)
                    labelRect = new RectF(x - 48, chartArea.Bottom + 4, 48, 16);
                    alignment = HorizontalAlignment.Right;
                }
                else
                {
                    labelRect = new RectF(x - 24, chartArea.Bottom + 4, 48, 16);
                    alignment = HorizontalAlignment.Center;
                }

                canvas.DrawString($"{distanceKm:F1} km", labelRect, alignment, VerticalAlignment.Top);
            }
        }
    }
}