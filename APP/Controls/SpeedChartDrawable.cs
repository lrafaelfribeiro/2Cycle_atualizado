using APP.Core;
using Microsoft.Maui.Graphics;

namespace APP.Controls
{
    public class SpeedChartDrawable : IDrawable
    {
        public List<SpeedSample> Samples { get; set; } = new();

        private const float YLabelWidth = 20f;
        private const float LabelGap = 4f;
        private const float BottomPadding = 26f; 
        private const float TopPadding = 14f;
        private const float RightPadding = 10f;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            if (Samples.Count < 2) return;

            var samples = Samples;

            // Se o primeiro registo não começa nos 0 km,
            // adiciona artificialmente o ponto inicial (0 km, 0 km/h).
            if (samples[0].DistanceKm > 0)
            {
                samples = new List<SpeedSample>(samples);
                samples.Insert(0, new SpeedSample(0, 0));
            }

            var smoothed = Smooth(samples, windowSize: 5);

            double maxSpeed = samples.Max(s => s.SpeedKmh);
            if (maxSpeed <= 0)
                maxSpeed = 5;

            maxSpeed = GetNiceMax(maxSpeed);

            double maxDistance = smoothed[^1].DistanceKm;
            if (maxDistance <= 0) maxDistance = 1;

            float chartLeft = YLabelWidth;
            float chartRight = dirtyRect.Width - RightPadding;
            float chartTop = TopPadding;
            float chartBottom = dirtyRect.Height - BottomPadding;
            float chartWidth = chartRight - chartLeft;
            float chartHeight = chartBottom - chartTop;

            float PointX(double distanceKm) => chartLeft + (float)(distanceKm / maxDistance * chartWidth);
            float PointY(double speedKmh) => chartTop + (float)(chartHeight - (speedKmh / maxSpeed * chartHeight));

            DrawYAxis(canvas, maxSpeed, chartLeft, chartTop, chartBottom, chartRight);
            DrawXAxis(canvas, maxDistance, chartLeft, chartRight, chartBottom);

            var fillPath = new PathF();
            fillPath.MoveTo(PointX(smoothed[0].DistanceKm), chartBottom);
            foreach (var sample in smoothed)
                fillPath.LineTo(PointX(sample.DistanceKm), PointY(sample.SpeedKmh));
            fillPath.LineTo(PointX(smoothed[^1].DistanceKm), chartBottom);
            fillPath.Close();

            canvas.FillColor = Color.FromArgb("#33A8E900");
            canvas.FillPath(fillPath);

            var linePath = new PathF();
            linePath.MoveTo(PointX(smoothed[0].DistanceKm), PointY(smoothed[0].SpeedKmh));
            foreach (var sample in smoothed.Skip(1))
                linePath.LineTo(PointX(sample.DistanceKm), PointY(sample.SpeedKmh));

            canvas.StrokeColor = Color.FromArgb("#A8E900");
            canvas.StrokeSize = 3;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.DrawPath(linePath);
        }

        private static void DrawYAxis(ICanvas canvas, double maxSpeed, float left, float top, float bottom, float right)
        {
            const int divisions = 4;

            canvas.FontColor = Color.FromArgb("#8A8D93");
            canvas.FontSize = 11;
            canvas.StrokeColor = Color.FromArgb("#1F2226");
            canvas.StrokeSize = 1;

            for (int i = 0; i <= divisions; i++)
            {
                double speed = maxSpeed * i / divisions;
                float y = bottom - (float)((bottom - top) * i / divisions);

                // linha de grelha horizontal, subtil
                canvas.DrawLine(left, y, right, y);

                // rótulo alinhado à direita, encostado ao eixo
                canvas.DrawString($"{speed:F0}", 0, y - 7, left - LabelGap, 14, HorizontalAlignment.Right, VerticalAlignment.Center);
            }
        }

        private static void DrawXAxis(
                            ICanvas canvas,
                            double maxDistance,
                            float left,
                            float right,
                            float bottom)
        {
            const int divisions = 4;
            const float labelWidth = 60f;

            canvas.FontColor = Color.FromArgb("#8A8D93");
            canvas.FontSize = 11;

            for (int i = 0; i <= divisions; i++)
            {
                double distance = maxDistance * i / divisions;
                float x = left + (right - left) * i / divisions;

                string label = distance < 10
                    ? $"{distance:F1} km"
                    : $"{distance:F0} km";

                HorizontalAlignment alignment;
                float textX;

                if (i == 0)
                {
                    alignment = HorizontalAlignment.Left;
                    textX = x;
                }
                else if (i == divisions)
                {
                    alignment = HorizontalAlignment.Right;
                    textX = x - labelWidth;
                }
                else
                {
                    alignment = HorizontalAlignment.Center;
                    textX = x - labelWidth / 2;
                }

                canvas.DrawString(
                    label,
                    textX,
                    bottom + 6,
                    labelWidth,
                    16,
                    alignment,
                    VerticalAlignment.Top);
            }
        }

        /// <summary>
        /// Média móvel simples — só para suavizar a apresentação do gráfico.
        /// Não altera os dados reais gravados (MaxSpeedKmh, médias, etc. continuam a vir do valor bruto).
        /// </summary>
        private static List<SpeedSample> Smooth(List<SpeedSample> samples, int windowSize)
        {
            if (samples.Count <= windowSize) return samples;

            var result = new List<SpeedSample>(samples.Count);
            for (int i = 0; i < samples.Count; i++)
            {
                int start = Math.Max(0, i - windowSize / 2);
                int end = Math.Min(samples.Count - 1, i + windowSize / 2);

                double avgSpeed = 0;
                for (int j = start; j <= end; j++)
                    avgSpeed += samples[j].SpeedKmh;
                avgSpeed /= (end - start + 1);

                result.Add(new SpeedSample(samples[i].DistanceKm, avgSpeed));
            }

            return result;
        }

        private static double GetNiceMax(double value)
        {
            if (value <= 20)
                return Math.Ceiling(value / 5) * 5;

            if (value <= 100)
                return Math.Ceiling(value / 10) * 10;

            return Math.Ceiling(value / 20) * 20;
        }
    }
}
