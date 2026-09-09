using APP.Core;
using APP.Services.Tiles;
using SkiaSharp;

namespace APP.Services.Thumbnails
{
    public class RouteThumbnailService : IRouteThumbnailService
    {
        private const int TileSize = TileMath.DefaultTileSize;

        // Thumbnails precisam de suportar rotas de qualquer tamanho (ao contrário
        // de "centrar no utilizador"), por isso o minZoom tem de ser bem mais baixo
        // que o default de TileMath (12) — senão rotas grandes ficam cortadas fora do canvas.
        private const int ThumbnailMinZoom = 2;
        private const int ThumbnailMaxZoom = 17;
        private const float ThumbnailBoundingBoxPaddingPx = 14f;

        // Stroke width escala com o zoom: com minZoom tão baixo, uma rota gigante
        // pode acabar em zoom 3-4, onde 3px fica quase invisível
        private const float MinRouteStrokeWidthPx = 3f;
        private const float MaxRouteStrokeWidthPx = 6f;
        private const int StrokeScaleMinZoom = 4;
        private const int StrokeScaleMaxZoom = 15;

        private const string RouteColorHex = "#A6E22E";
        private const string RouteHaloColorHex = "#1E1E1E";
        private const float RouteHaloExtraWidthPx = 2.5f;

        private readonly ITileCacheService _tileCache;
        private readonly string _cacheDir;

        public RouteThumbnailService(ITileCacheService tileCache)
        {
            _tileCache = tileCache;
            _cacheDir = Path.Combine(FileSystem.Current.AppDataDirectory, "route-thumbnails");
            Directory.CreateDirectory(_cacheDir);
        }

        public async Task<string?> GetOrCreateThumbnailPathAsync(
            Guid routeId,
            IReadOnlyList<(double Lat, double Lon)> points,
            int widthPx = 180,
            int heightPx = 140,
            CancellationToken ct = default)
        {
            string path = Path.Combine(_cacheDir, $"{routeId}.png");
            if (File.Exists(path))
                return path; // cache-first: nunca recompõe a mesma rota duas vezes

            if (points == null || points.Count < 2)
                return null;

            double minLat = points.Min(p => p.Lat);
            double maxLat = points.Max(p => p.Lat);
            double minLon = points.Min(p => p.Lon);
            double maxLon = points.Max(p => p.Lon);

            int zoom = TileMath.CalculateZoomForRoute(
                minLat, maxLat, minLon, maxLon, widthPx, heightPx,
                minZoom: ThumbnailMinZoom,
                maxZoom: ThumbnailMaxZoom,
                padding: ThumbnailBoundingBoxPaddingPx);

            double centerLat = (minLat + maxLat) / 2.0;
            double centerLon = (minLon + maxLon) / 2.0;
            var (centerX, centerY) = TileMath.LatLonToPixel(centerLat, centerLon, zoom);

            double originX = centerX - widthPx / 2.0;
            double originY = centerY - heightPx / 2.0;

            using var surface = SKSurface.Create(new SKImageInfo(widthPx, heightPx));
            var canvas = surface.Canvas;
            canvas.Clear(SKColors.Transparent);

            await DrawTilesAsync(canvas, originX, originY, widthPx, heightPx, zoom, ct);
            DrawRoutePolyline(canvas, points, zoom, originX, originY);

            using var image = surface.Snapshot();
            using var data = image.Encode(SKEncodedImageFormat.Png, 90);
            await using var fs = File.Create(path);
            data.SaveTo(fs);

            return path;
        }

        private async Task DrawTilesAsync(
            SKCanvas canvas, double originX, double originY,
            int widthPx, int heightPx, int zoom, CancellationToken ct)
        {
            int firstTileX = (int)Math.Floor(originX / TileSize);
            int firstTileY = (int)Math.Floor(originY / TileSize);
            int lastTileX = (int)Math.Floor((originX + widthPx) / TileSize);
            int lastTileY = (int)Math.Floor((originY + heightPx) / TileSize);
            int tileCount = 1 << zoom;

            for (int tx = firstTileX; tx <= lastTileX; tx++)
            {
                int wrappedX = ((tx % tileCount) + tileCount) % tileCount; // longitude dá a volta

                for (int ty = firstTileY; ty <= lastTileY; ty++)
                {
                    ct.ThrowIfCancellationRequested();
                    if (ty < 0 || ty >= tileCount)
                        continue;

                    byte[]? bytes = await _tileCache.GetTileAsync(wrappedX, ty, zoom);
                    if (bytes == null)
                        continue;

                    using var bitmap = SKBitmap.Decode(bytes);
                    if (bitmap == null)
                        continue;

                    float dx = (float)(tx * TileSize - originX);
                    float dy = (float)(ty * TileSize - originY);
                    canvas.DrawBitmap(bitmap, dx, dy);
                }
            }
        }

        private void DrawRoutePolyline(
    SKCanvas canvas, IReadOnlyList<(double Lat, double Lon)> points,
    int zoom, double originX, double originY)
        {
            using var path = BuildRoutePath(points, zoom, originX, originY);
            float strokeWidth = CalculateStrokeWidthForZoom(zoom);

            // Halo escuro por baixo -> garante contraste em qualquer tile (claro ou escuro)
            using var haloPaint = CreateRoutePaint(RouteHaloColorHex, strokeWidth + RouteHaloExtraWidthPx);
            canvas.DrawPath(path, haloPaint);

            using var routePaint = CreateRoutePaint(RouteColorHex, strokeWidth);
            canvas.DrawPath(path, routePaint);
        }

        private static SKPath BuildRoutePath(
            IReadOnlyList<(double Lat, double Lon)> points, int zoom, double originX, double originY)
        {
            var path = new SKPath();

            for (int i = 0; i < points.Count; i++)
            {
                var (px, py) = TileMath.LatLonToPixel(points[i].Lat, points[i].Lon, zoom);
                float x = (float)(px - originX);
                float y = (float)(py - originY);

                if (i == 0)
                    path.MoveTo(x, y);
                else
                    path.LineTo(x, y);
            }

            return path;
        }

        private static SKPaint CreateRoutePaint(string colorHex, float strokeWidth) => new()
        {
            Color = SKColor.Parse(colorHex),
            StrokeWidth = strokeWidth,
            Style = SKPaintStyle.Stroke,
            IsAntialias = true,
            StrokeCap = SKStrokeCap.Round,
            StrokeJoin = SKStrokeJoin.Round
        };

        private static float CalculateStrokeWidthForZoom(int zoom)
        {
            if (zoom <= StrokeScaleMinZoom) return MaxRouteStrokeWidthPx;
            if (zoom >= StrokeScaleMaxZoom) return MinRouteStrokeWidthPx;

            double t = (double)(zoom - StrokeScaleMinZoom) / (StrokeScaleMaxZoom - StrokeScaleMinZoom);
            return (float)(MaxRouteStrokeWidthPx - t * (MaxRouteStrokeWidthPx - MinRouteStrokeWidthPx));
        }
    }
}
