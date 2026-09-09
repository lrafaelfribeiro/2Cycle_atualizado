using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics;

namespace APP.Core
{
    public class ShareCardDrawable : IDrawable
    {
        private static readonly Microsoft.Maui.Graphics.Font RegularFont = new("InterRegular");
        private static readonly Microsoft.Maui.Graphics.Font ExtraBoldFont = new("InterExtraBold");

        public string DistanceDisplay { get; set; } = string.Empty;
        public string DurationDisplay { get; set; } = string.Empty;
        public string ElevationDisplay { get; set; } = string.Empty;
        public List<Location> RoutePoints { get; set; } = new();

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();

            float width = dirtyRect.Width;
            float height = dirtyRect.Height;

            // Margens proporcionais ao ecra
            float horizontalMargin = width * 0.08f;
            float topMargin = height * 0.05f;
            float bottomMargin = height * 0.05f;

            // Área das estatísticas
            float statsHeight = height * 0.5f;

            // Espaçamento entre estatísticas
            float statSpacing = height * 0.03f;

            // Cada estatística ocupa 1/3 dessa área
            float statBlockHeight =
                    (statsHeight - statSpacing * 2) / 3f;

            // Estatísticas — em linha no meio do ecra
            float statsY = topMargin;

            DrawStat(canvas, "DISTÂNCIA", DistanceDisplay, width, statsY, statBlockHeight);
            statsY += statBlockHeight + statSpacing;

            DrawStat(canvas, "TEMPO", DurationDisplay, width, statsY, statBlockHeight);
            statsY += statBlockHeight + statSpacing;

            DrawStat(canvas, "ELEVAÇÃO", ElevationDisplay, width, statsY, statBlockHeight);
            statsY += statBlockHeight + statSpacing;

            // Percurso — área central
            float routeTop = topMargin + statsHeight;

            float routeHeight = height - routeTop - bottomMargin;

            if (routeHeight > 0)
            {
                var routeArea = new RectF(
                    horizontalMargin,
                    routeTop,
                    width - horizontalMargin * 2,
                    routeHeight
                );

                DrawRoute(canvas, routeArea);
            }


            canvas.RestoreState();
        }

        private void DrawRoute(ICanvas canvas, RectF area)
        {
            if (RoutePoints.Count < 2) return;

            var latitudes = RoutePoints.Select(p => p.Latitude);
            var longitudes = RoutePoints.Select(p => p.Longitude);

            double minLat = latitudes.Min();
            double maxLat = latitudes.Max();
            double minLon = longitudes.Min();
            double maxLon = longitudes.Max();

            double latSpan = Math.Max(maxLat - minLat, 0.0001);
            double lonSpan = Math.Max(maxLon - minLon, 0.0001);

            double scale = Math.Min(area.Width / lonSpan, area.Height / latSpan) * 0.85;
            float offsetX = area.X + (area.Width - (float)(lonSpan * scale)) / 2;
            float offsetY = area.Y + (area.Height - (float)(latSpan * scale)) / 2;

            float X(double lon) => offsetX + (float)((lon - minLon) * scale);
            float Y(double lat) => offsetY + (float)((maxLat - lat) * scale);

            var path = new PathF();
            path.MoveTo(X(RoutePoints[0].Longitude), Y(RoutePoints[0].Latitude));
            foreach (var point in RoutePoints.Skip(1))
                path.LineTo(X(point.Longitude), Y(point.Latitude));

            canvas.StrokeColor = Color.FromArgb("#A6E22E");
            canvas.StrokeSize = 8;
            canvas.StrokeLineJoin = LineJoin.Round;
            canvas.StrokeLineCap = LineCap.Round;
            canvas.DrawPath(path);

            canvas.FillColor = Colors.White;
            canvas.FillCircle(X(RoutePoints[0].Longitude), Y(RoutePoints[0].Latitude), 10);
            canvas.FillColor = Color.FromArgb("#A6E22E");
            canvas.FillCircle(X(RoutePoints[^1].Longitude), Y(RoutePoints[^1].Latitude), 10);
        }

        private void DrawStat(ICanvas canvas, string label, string value, float cardWidth, float y, float blockHeight)
        {
            // LABEL 
            canvas.Font = RegularFont;

            float labelFontSize = Math.Clamp(blockHeight * 0.35f, 20f, 40f);
            canvas.FontSize = labelFontSize;

            canvas.FontColor = Color.FromArgb("#8A8D93");
            canvas.DrawString(label, 0, y, cardWidth, blockHeight * 0.3f, HorizontalAlignment.Center, VerticalAlignment.Top);

            // VALUE
            canvas.Font = ExtraBoldFont;

            float valueFontSize = Math.Clamp(blockHeight * 0.5f, 50f, 150f);
            canvas.FontSize = valueFontSize;

            canvas.FontColor = Colors.White;
            canvas.DrawString(value, 0, y + blockHeight * 0.3f, cardWidth, blockHeight * 0.7f, HorizontalAlignment.Center, VerticalAlignment.Top);
        }
    }
}
