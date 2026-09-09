using System.Collections.Concurrent;

namespace APP.Services.Tiles
{
    public class TileCacheService : ITileCacheService
    {
        private readonly HttpClient _httpClient;

        private readonly ConcurrentDictionary<
            string,
            Lazy<Task<byte[]?>>
        > _memoryCache = new();

        public TileCacheService(HttpClient httpClient)
        {
            _httpClient = httpClient;

            _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(
                "2Cycle-App/1.0 (projeto academico)");
        }

        public async Task<byte[]?> GetTileAsync(
            int x,
            int y,
            int zoom)
        {
            string key = $"{zoom}/{x}/{y}";

            var lazyTask = _memoryCache.GetOrAdd(
                key,
                _ => new Lazy<Task<byte[]?>>(
                    () => DownloadTileAsync(x, y, zoom),
                    LazyThreadSafetyMode.ExecutionAndPublication));

            return await lazyTask.Value;
        }

        private async Task<byte[]?> DownloadTileAsync(
            int x,
            int y,
            int zoom)
        {
            string url = $"{zoom}/{x}/{y}.png";

            try
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[TileCache] DOWNLOAD {url}");

                var bytes =
                    await _httpClient.GetByteArrayAsync(url);

                return bytes;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"[TileCache] ERRO {url}: " +
                    $"{ex.GetType().Name} - {ex.Message}");

                return null;
            }
        }
    }
}
