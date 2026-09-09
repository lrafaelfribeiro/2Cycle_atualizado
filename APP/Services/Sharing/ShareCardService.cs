using APP.Core;
using APP.Models;
using Microsoft.Maui.Graphics.Skia;

namespace APP.Services.Sharing
{
    public class ShareCardService : IShareCardService
    {
        private const int Width = 1080;
        private const int Height = 1350; // proporção 4:5
        public async Task<string> GenerateShareCardAsync(ActivityListItem item)
        {
            using var exportContext = new SkiaBitmapExportContext(Width, Height, 1f);
            var canvas = exportContext.Canvas;

            var drawable = new ShareCardDrawable
            {
                DistanceDisplay = item.DistanceDisplay,
                DurationDisplay = item.DurationDisplay,
                ElevationDisplay = item.ElevationDisplay,
                RoutePoints = item.RoutePoints
            };

            drawable.Draw(canvas, new RectF(0, 0, Width, Height));

            string path = Path.Combine(FileSystem.CacheDirectory, $"share_{item.Id}.png");
            using var stream = File.Create(path);
            exportContext.WriteToStream(stream);

            return path;
        }
    }
}
