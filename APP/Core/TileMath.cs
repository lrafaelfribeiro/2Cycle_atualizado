using System;

namespace APP.Core
{
    public static class TileMath
    {
        public const int DefaultTileSize = 256;

        // Web Mercator suporta aproximadamente esta latitude máxima.
        public const double MaxLatitude = 85.05112878;

        public static int CalculateZoomForRoute(
            double minLat,
            double maxLat,
            double minLon,
            double maxLon,
            double viewWidth,
            double viewHeight,
            int minZoom = 12,
            int maxZoom = 17,
            float padding = 10f)
        {
            if (viewWidth <= 0)
                viewWidth = 90;

            if (viewHeight <= 0)
                viewHeight = 90;

            viewWidth = Math.Max(1, viewWidth - padding * 2);
            viewHeight = Math.Max(1, viewHeight - padding * 2);

            minLat = Math.Clamp(minLat, -MaxLatitude, MaxLatitude);
            maxLat = Math.Clamp(maxLat, -MaxLatitude, MaxLatitude);

            // Testamos do zoom máximo para baixo.
            // O primeiro que conseguir conter a rota é escolhido.
            for (int zoom = maxZoom; zoom >= minZoom; zoom--)
            {
                var topLeft = LatLonToPixel(
                    maxLat,
                    minLon,
                    zoom);

                var bottomRight = LatLonToPixel(
                    minLat,
                    maxLon,
                    zoom);

                double width = Math.Abs(bottomRight.X - topLeft.X);
                double height = Math.Abs(bottomRight.Y - topLeft.Y);

                if (width <= viewWidth && height <= viewHeight)
                    return zoom;
            }

            return minZoom;
        }

        public static (int X, int Y) LatLonToTile(
            double lat,
            double lon,
            int zoom)
        {
            lat = Math.Clamp(lat, -MaxLatitude, MaxLatitude);

            double latRad = lat * Math.PI / 180.0;
            int n = 1 << zoom;

            int x = (int)Math.Floor(
                (lon + 180.0) / 360.0 * n);

            int y = (int)Math.Floor(
                (1.0 -
                 Math.Log(
                     Math.Tan(latRad) +
                     1.0 / Math.Cos(latRad)
                 ) / Math.PI) / 2.0 * n);

            // X pode dar a volta ao mundo.
            x = Mod(x, n);

            y = Math.Clamp(y, 0, n - 1);

            return (x, y);
        }

        public static (double X, double Y) LatLonToPixel(
            double lat,
            double lon,
            int zoom,
            int tileSize = DefaultTileSize)
        {
            lat = Math.Clamp(lat, -MaxLatitude, MaxLatitude);

            double latRad = lat * Math.PI / 180.0;
            double worldSize = (1 << zoom) * tileSize;

            double x =
                (lon + 180.0) / 360.0 * worldSize;

            double y =
                (1.0 -
                 Math.Log(
                     Math.Tan(latRad) +
                     1.0 / Math.Cos(latRad)
                 ) / Math.PI)
                / 2.0
                * worldSize;

            return (x, y);
        }

        public static double GetWorldSize(
            int zoom,
            int tileSize = DefaultTileSize)
        {
            return (1 << zoom) * tileSize;
        }

        private static int Mod(int value, int modulus)
        {
            return ((value % modulus) + modulus) % modulus;
        }
    }
}
