using APP.Core;
using APP.Services.Tiles;
using Microsoft.Maui.Devices.Sensors;
using Microsoft.Maui.Graphics.Platform;
using System;
using System.Collections.Generic;
using System.Linq;

namespace APP.Controls
{
    public class RouteMiniMapView : GraphicsView
    {
        private readonly ActivityRouteDrawable _drawable = new();

        private static ITileCacheService? _tileCacheService;

        private CancellationTokenSource? _loadCancellation;

        public static readonly BindableProperty RoutePointsProperty =
            BindableProperty.Create(
                nameof(RoutePoints),
                typeof(List<Location>),
                typeof(RouteMiniMapView),
                defaultValue: null,
                propertyChanged: OnRoutePointsChanged);

        public List<Location>? RoutePoints
        {
            get => (List<Location>?)GetValue(RoutePointsProperty);
            set => SetValue(RoutePointsProperty, value);
        }

        public RouteMiniMapView()
        {
            Drawable = _drawable;

            _tileCacheService ??=
                IPlatformApplication.Current?
                    .Services
                    .GetService<ITileCacheService>();

            HeightRequest = 90;
            WidthRequest = 90;
        }

        private static void OnRoutePointsChanged(
            BindableObject bindable,
            object oldValue,
            object newValue)
        {
            if (bindable is not RouteMiniMapView view)
                return;

            var points =
                (newValue as List<Location>)
                ?? new List<Location>();

            view._loadCancellation?.Cancel();

            view._loadCancellation =
                new CancellationTokenSource();

            view._drawable.Points = points;
            view._drawable.Tiles.Clear();

            view.Invalidate();

            if (points.Count < 2 ||
                _tileCacheService is null)
            {
                return;
            }

            _ = view.LoadTilesAsync(
                points,
                view._loadCancellation.Token);
        }

        private async Task LoadTilesAsync(
            List<Location> points,
            CancellationToken cancellationToken)
        {
            try
            {
                double minLat =
                    points.Min(p => p.Latitude);

                double maxLat =
                    points.Max(p => p.Latitude);

                double minLon =
                    points.Min(p => p.Longitude);

                double maxLon =
                    points.Max(p => p.Longitude);

                double width =
                    Width > 0
                        ? Width
                        : 90f;

                double height =
                    Height > 0
                        ? Height
                        : 90f;

                int zoom =
                    TileMath.CalculateZoomForRoute(
                        minLat,
                        maxLat,
                        minLon,
                        maxLon,
                        width,
                        height,
                        minZoom: 12,
                        maxZoom: 17,
                        padding: 10f);

                _drawable.Zoom = zoom;

                int tileSize =
                    TileMath.DefaultTileSize;

                var topLeft =
                    TileMath.LatLonToPixel(
                        maxLat,
                        minLon,
                        zoom,
                        tileSize);

                var bottomRight =
                    TileMath.LatLonToPixel(
                        minLat,
                        maxLon,
                        zoom,
                        tileSize);

                // Expandimos uma pequena margem para garantir
                // que o mapa cobre também a zona de padding.
                double extraPixels = 0;

                double minPixelX =
                    topLeft.X - extraPixels;

                double maxPixelX =
                    bottomRight.X + extraPixels;

                double minPixelY =
                    topLeft.Y - extraPixels;

                double maxPixelY =
                    bottomRight.Y + extraPixels;

                int minTileX =
                    (int)Math.Floor(
                        minPixelX / tileSize);

                int maxTileX =
                    (int)Math.Floor(
                        maxPixelX / tileSize);

                int minTileY =
                    (int)Math.Floor(
                        minPixelY / tileSize);

                int maxTileY =
                    (int)Math.Floor(
                        maxPixelY / tileSize);

                int tilesPerWorld =
                    1 << zoom;

                // Evita carregar uma quantidade absurda
                // de tiles em situações inesperadas.
                minTileY =
                    Math.Clamp(
                        minTileY,
                        0,
                        tilesPerWorld - 1);

                maxTileY =
                    Math.Clamp(
                        maxTileY,
                        0,
                        tilesPerWorld - 1);

                var tileRequests =
                    new List<Task<ActivityRouteDrawable.TileData?>>();

                for (int tileY = minTileY;
                     tileY <= maxTileY;
                     tileY++)
                {
                    for (int tileX = minTileX;
                         tileX <= maxTileX;
                         tileX++)
                    {
                        int wrappedX =
                            Mod(tileX, tilesPerWorld);

                        tileRequests.Add(
                            LoadTileAsync(
                                wrappedX,
                                tileY,
                                zoom,
                                cancellationToken));
                    }
                }

                var loadedTiles =
                    await Task.WhenAll(tileRequests);

                cancellationToken.ThrowIfCancellationRequested();

                var tiles =
                    loadedTiles
                        .Where(t => t is not null)
                        .Cast<ActivityRouteDrawable.TileData>()
                        .ToList();

                _drawable.Tiles = tiles;

                Invalidate();
            }
            catch (OperationCanceledException)
            {
                // Uma nova lista de pontos chegou.
                // Ignoramos o carregamento anterior.
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RouteMiniMap] Erro ao carregar mapa: " +
                    $"{ex.GetType().Name} - {ex.Message}");
            }
        }

        private async Task<ActivityRouteDrawable.TileData?> LoadTileAsync(
            int x,
            int y,
            int zoom,
            CancellationToken cancellationToken)
        {
            if (_tileCacheService is null)
                return null;

            try
            {
                cancellationToken.ThrowIfCancellationRequested();

                var bytes =
                    await _tileCacheService.GetTileAsync(
                        x,
                        y,
                        zoom);

                cancellationToken.ThrowIfCancellationRequested();

                if (bytes is null || bytes.Length == 0)
                    return null;

                var image =
                    PlatformImage.FromStream(
                        new MemoryStream(bytes));

                return new ActivityRouteDrawable.TileData
                {
                    X = x,
                    Y = y,
                    Image = image
                };
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[RouteMiniMap] Erro tile " +
                    $"{zoom}/{x}/{y}: " +
                    $"{ex.GetType().Name} - {ex.Message}");

                return null;
            }
        }

        private static int Mod(
            int value,
            int modulus)
        {
            return ((value % modulus) + modulus) % modulus;
        }

        protected override void OnHandlerChanged()
        {
            if (Handler is null)
            {
                _loadCancellation?.Cancel();
            }

            base.OnHandlerChanged();
        }
    }
}
