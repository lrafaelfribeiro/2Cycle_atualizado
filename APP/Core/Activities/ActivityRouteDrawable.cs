using APP.Core;
using Microsoft.Maui.Graphics;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APP.Controls
{
    public class ActivityRouteDrawable : IDrawable
    {
        public class TileData
        {
            public int X { get; set; }
            public int Y { get; set; }
            public Microsoft.Maui.Graphics.IImage Image { get; set; } = null!;
        }

        public List<Location> Points { get; set; } = new();

        public List<TileData> Tiles { get; set; } = new();

        public int Zoom { get; set; } = 15;

        public void Draw(ICanvas canvas, RectF dirtyRect)
        {
            canvas.SaveState();

            try
            {
                if (Points.Count < 2)
                    return;

                DrawMap(canvas, dirtyRect);
                DrawRoute(canvas, dirtyRect);
            }
            finally
            {
                canvas.RestoreState();
            }
        }

        private void DrawMap(
            ICanvas canvas,
            RectF dirtyRect)
        {
            if (Tiles.Count == 0)
                return;

            var bounds = GetRouteBounds();

            var topLeft = TileMath.LatLonToPixel(
                bounds.MaxLat,
                bounds.MinLon,
                Zoom);

            var bottomRight = TileMath.LatLonToPixel(
                bounds.MinLat,
                bounds.MaxLon,
                Zoom);

            double routeWidth =
                Math.Max(
                    1,
                    bottomRight.X - topLeft.X);

            double routeHeight =
                Math.Max(
                    1,
                    bottomRight.Y - topLeft.Y);

            const float padding = 10f;

            float availableWidth =
                Math.Max(
                    1,
                    dirtyRect.Width - padding * 2);

            float availableHeight =
                Math.Max(
                    1,
                    dirtyRect.Height - padding * 2);

            double scale = Math.Min(
                availableWidth / routeWidth,
                availableHeight / routeHeight);

            float offsetX =
                dirtyRect.X +
                padding +
                (availableWidth -
                 (float)(routeWidth * scale)) / 2f;

            float offsetY =
                dirtyRect.Y +
                padding +
                (availableHeight -
                 (float)(routeHeight * scale)) / 2f;

            int tileSize = TileMath.DefaultTileSize;

            foreach (var tile in Tiles)
            {
                double tilePixelX =
                    tile.X * tileSize;

                double tilePixelY =
                    tile.Y * tileSize;

                float x =
                    offsetX +
                    (float)((tilePixelX - topLeft.X) * scale);

                float y =
                    offsetY +
                    (float)((tilePixelY - topLeft.Y) * scale);

                float width =
                    (float)(tileSize * scale);

                float height =
                    (float)(tileSize * scale);

                canvas.DrawImage(
                    tile.Image,
                    x,
                    y,
                    width,
                    height);
            }

            // Escurecimento ligeiro para a rota ficar mais visível.
            canvas.FillColor =
                Color.FromArgb("#55000000");

            canvas.FillRectangle(dirtyRect);
        }

        private void DrawRoute(
            ICanvas canvas,
            RectF dirtyRect)
        {
            if (Points.Count < 2)
                return;

            var bounds = GetRouteBounds();

            var topLeft = TileMath.LatLonToPixel(
                bounds.MaxLat,
                bounds.MinLon,
                Zoom);

            var bottomRight = TileMath.LatLonToPixel(
                bounds.MinLat,
                bounds.MaxLon,
                Zoom);

            double routeWidth =
                Math.Max(
                    1,
                    bottomRight.X - topLeft.X);

            double routeHeight =
                Math.Max(
                    1,
                    bottomRight.Y - topLeft.Y);

            const float padding = 10f;

            float availableWidth =
                Math.Max(
                    1,
                    dirtyRect.Width - padding * 2);

            float availableHeight =
                Math.Max(
                    1,
                    dirtyRect.Height - padding * 2);

            double scale = Math.Min(
                availableWidth / routeWidth,
                availableHeight / routeHeight);

            float offsetX =
                dirtyRect.X +
                padding +
                (availableWidth -
                 (float)(routeWidth * scale)) / 2f;

            float offsetY =
                dirtyRect.Y +
                padding +
                (availableHeight -
                 (float)(routeHeight * scale)) / 2f;

            float X(double lon, double lat)
            {
                var pixel =
                    TileMath.LatLonToPixel(
                        lat,
                        lon,
                        Zoom);

                return offsetX +
                    (float)((pixel.X - topLeft.X) * scale);
            }

            float Y(double lon, double lat)
            {
                var pixel =
                    TileMath.LatLonToPixel(
                        lat,
                        lon,
                        Zoom);

                return offsetY +
                    (float)((pixel.Y - topLeft.Y) * scale);
            }

            var path = new PathF();

            var first = Points[0];

            path.MoveTo(
                X(first.Longitude, first.Latitude),
                Y(first.Longitude, first.Latitude));

            foreach (var point in Points.Skip(1))
            {
                path.LineTo(
                    X(point.Longitude, point.Latitude),
                    Y(point.Longitude, point.Latitude));
            }

            canvas.StrokeColor =
                Color.FromArgb("#A8E900");

            canvas.StrokeSize = 3f;

            canvas.StrokeLineJoin =
                LineJoin.Round;

            canvas.StrokeLineCap =
                LineCap.Round;

            canvas.DrawPath(path);
        }

        private (
            double MinLat,
            double MaxLat,
            double MinLon,
            double MaxLon
        ) GetRouteBounds()
        {
            return (
                Points.Min(p => p.Latitude),
                Points.Max(p => p.Latitude),
                Points.Min(p => p.Longitude),
                Points.Max(p => p.Longitude)
            );
        }
    }
}
