using API.Core;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.AspNetCore.RateLimiting;
using System.Diagnostics;
using System.Globalization;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [EnableRateLimiting(RateLimitPolicyNames.Maps)]
    public class MapsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IMemoryCache _cache;

        // Máximo de 3 pedidos simultâneos à DGT
        private static readonly SemaphoreSlim DgtSemaphore =
            new(3, 3);

        public MapsController(
            IHttpClientFactory httpClientFactory,
            IMemoryCache cache)
        {
            _httpClientFactory = httpClientFactory;
            _cache = cache;
        }

        [HttpGet("dgt-ortos/{z:int}/{x:int}/{y:int}")]
        public async Task<IActionResult> GetOrtoTile(
    int z,
    int x,
    int y,
    CancellationToken cancellationToken)
        {
            if (!IsValidTileCoordinate(z, x, y))
                return Problem(title: "Coordenadas de tile inválidas.", statusCode: StatusCodes.Status400BadRequest);

            var cacheKey = $"dgt-ortos:{z}:{x}:{y}";

            // =========================================================
            // CACHE
            // =========================================================

            if (_cache.TryGetValue(cacheKey, out byte[]? cachedBytes))
            {
                Console.WriteLine(
                    $"DGT CACHE {z}/{x}/{y} | {cachedBytes!.Length} bytes");

                return File(cachedBytes, "image/jpeg");
            }


            // =========================================================
            // XYZ -> EPSG:3857 BBOX
            // =========================================================

            const double originShift = 20037508.342789244;

            var tileSize =
                2 * originShift / Math.Pow(2, z);

            var minX =
                -originShift + x * tileSize;

            var maxX =
                -originShift + (x + 1) * tileSize;

            var maxY =
                originShift - y * tileSize;

            var minY =
                originShift - (y + 1) * tileSize;

            var bbox =
                $"{minX.ToString(CultureInfo.InvariantCulture)}," +
                $"{minY.ToString(CultureInfo.InvariantCulture)}," +
                $"{maxX.ToString(CultureInfo.InvariantCulture)}," +
                $"{maxY.ToString(CultureInfo.InvariantCulture)}";


            // =========================================================
            // DGT WMS
            // =========================================================

            var wmsUrl =
                "https://cartografia.dgterritorio.gov.pt/wms/ortos2021" +
                "?SERVICE=WMS" +
                "&VERSION=1.3.0" +
                "&REQUEST=GetMap" +
                "&LAYERS=Ortos2021-RGB" +
                "&STYLES=" +
                "&FORMAT=image/jpeg" +
                "&TRANSPARENT=false" +
                "&CRS=EPSG:3857" +
                "&WIDTH=256" +
                "&HEIGHT=256" +
                $"&BBOX={bbox}";


            await DgtSemaphore.WaitAsync(cancellationToken);

            try
            {
                // =====================================================
                // IMPORTANTE:
                // verificar novamente o cache depois do Semaphore
                //
                // Outro request pode ter descarregado a mesma tile
                // enquanto este estava à espera.
                // =====================================================

                if (_cache.TryGetValue(
                    cacheKey,
                    out byte[]? cachedAfterWait))
                {
                    Console.WriteLine(
                        $"DGT CACHE AFTER WAIT {z}/{x}/{y}");

                    return File(
                        cachedAfterWait!,
                        "image/jpeg");
                }


                var client =
                    _httpClientFactory.CreateClient();


                var stopwatch =
                    System.Diagnostics.Stopwatch.StartNew();


                using var response =
                    await client.GetAsync(
                        wmsUrl,
                        cancellationToken);


                var bytes =
                    await response.Content.ReadAsByteArrayAsync(
                        cancellationToken);


                stopwatch.Stop();


                Console.WriteLine(
                    $"DGT ZXY {z}/{x}/{y} | " +
                    $"{stopwatch.ElapsedMilliseconds} ms | " +
                    $"{bytes.Length} bytes");


                if (!response.IsSuccessStatusCode)
                {
                    return StatusCode(
                        (int)response.StatusCode);
                }


                // =====================================================
                // NÃO GUARDAR RESPOSTAS VAZIAS
                // =====================================================

                if (bytes.Length < 1000)
                {
                    Console.WriteLine(
                        $"DGT TILE VAZIA {z}/{x}/{y}");

                    return File(
                        bytes,
                        "image/jpeg");
                }


                // =====================================================
                // CACHE 6 HORAS
                // =====================================================

                _cache.Set(cacheKey, bytes, new MemoryCacheEntryOptions
                {
                    AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(6),
                    Size = 1
                });


                Console.WriteLine(
                    $"DGT CACHE SAVE {z}/{x}/{y}");


                return File(
                    bytes,
                    "image/jpeg");
            }
            finally
            {
                DgtSemaphore.Release();
            }
        }



        [HttpGet("dgt-ortos-tile")]
        public async Task<IActionResult> GetOrtoTile(
            [FromQuery] string bbox,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(bbox))
                return BadRequest("bbox obrigatório.");

            // =====================================================
            // CACHE
            // =====================================================

            if (!TryNormalizeBbox(bbox, out var normalizedBbox))
                return Problem(title: "bbox inválido.", statusCode: StatusCodes.Status400BadRequest);

            var cacheKey = $"dgt-ortos:{normalizedBbox}";

            if (_cache.TryGetValue(
                    cacheKey,
                    out byte[]? cachedBytes))
            {
                Console.WriteLine(
                    $"DGT CACHE: {normalizedBbox}");

                Response.Headers.CacheControl =
                    "public,max-age=604800";

                return File(
                    cachedBytes!,
                    "image/jpeg");
            }

            // =====================================================
            // ESPERAR PELA DGT
            // =====================================================

            await DgtSemaphore.WaitAsync(
                cancellationToken);

            try
            {
                // =================================================
                // VERIFICAR CACHE NOVAMENTE
                // =================================================

                if (_cache.TryGetValue(
                        cacheKey,
                        out cachedBytes))
                {
                    Console.WriteLine(
                            $"DGT CACHE (2): {normalizedBbox}");

                    return File(
                        cachedBytes!,
                        "image/jpeg");
                }

                var client =
                    _httpClientFactory.CreateClient();

                // =================================================
                // URL DGT
                // =================================================

                var wmsUrl =
                    "https://cartografia.dgterritorio.gov.pt/wms/ortos2021" +
                    "?SERVICE=WMS" +
                    "&VERSION=1.3.0" +
                    "&REQUEST=GetMap" +
                    "&LAYERS=Ortos2021-RGB" +
                    "&STYLES=" +
                    "&FORMAT=image/jpeg" +
                    "&TRANSPARENT=false" +
                    "&CRS=EPSG:3857" +
                    "&WIDTH=256" +
                    "&HEIGHT=256" +
                    $"&BBOX={normalizedBbox}";

                // =================================================
                // RETRY
                // =================================================

                const int maxAttempts = 3;

                for (int attempt = 1;
                     attempt <= maxAttempts;
                     attempt++)
                {
                    try
                    {
                        var sw =
                            Stopwatch.StartNew();

                        Console.WriteLine(
                            $"DGT REQUEST {attempt}/{maxAttempts}: {normalizedBbox}");

                        using var response =
                            await client.GetAsync(
                                wmsUrl,
                                cancellationToken);

                        var bytes =
                            await response.Content
                                .ReadAsByteArrayAsync(
                                    cancellationToken);

                        sw.Stop();

                        Console.WriteLine(
                            $"DGT RESPONSE: " +
                            $"{sw.ElapsedMilliseconds} ms | " +
                            $"{response.StatusCode} | " +
                            $"{bytes.Length} bytes");

                        if (!response.IsSuccessStatusCode)
                        {
                            if (attempt == maxAttempts)
                            {
                                return StatusCode(
                                    (int)response.StatusCode,
                                    "Falha ao obter tile da DGT.");
                            }

                            await Task.Delay(
                                300 * attempt,
                                cancellationToken);

                            continue;
                        }

                        // =============================================
                        // VALIDAR IMAGEM
                        // =============================================

                        if (bytes.Length < 5000)
                        {
                            Console.WriteLine(
                                "DGT: imagem demasiado pequena, retry.");

                            if (attempt == maxAttempts)
                            {
                                return File(
                                    bytes,
                                    "image/jpeg");
                            }

                            await Task.Delay(
                                500 * attempt,
                                cancellationToken);

                            continue;
                        }

                        // =============================================
                        // CACHE
                        // =============================================

                        _cache.Set(cacheKey, bytes, new MemoryCacheEntryOptions
                        {
                            AbsoluteExpirationRelativeToNow = TimeSpan.FromDays(7),
                            Size = 1
                        });

                        Response.Headers.CacheControl =
                            "public,max-age=604800";

                        return File(
                            bytes,
                            "image/jpeg");
                    }
                    catch (HttpRequestException ex)
                    {
                        Console.WriteLine(
                            $"DGT ERROR {attempt}: " +
                            ex.Message);

                        if (attempt == maxAttempts)
                            throw;

                        await Task.Delay(
                            500 * attempt,
                            cancellationToken);
                    }
                }

                return StatusCode(
                    502,
                    "Não foi possível obter a imagem da DGT.");
            }
            finally
            {
                DgtSemaphore.Release();
            }
        }

        private static bool IsValidTileCoordinate(int z, int x, int y)
        {
            if (z is < 0 or > 22)
                return false;

            var tilesPerAxis = 1L << z;
            return x >= 0 && y >= 0 && x < tilesPerAxis && y < tilesPerAxis;
        }

        private static bool TryNormalizeBbox(string? bbox, out string normalizedBbox)
        {
            normalizedBbox = string.Empty;
            if (string.IsNullOrWhiteSpace(bbox))
                return false;

            var values = bbox.Split(',', StringSplitOptions.TrimEntries);
            if (values.Length != 4 || !values.All(value => double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out _)))
                return false;

            var coordinates = values.Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();
            const double maxWebMercator = 20037508.342789244;
            if (coordinates.Any(value => !double.IsFinite(value) || Math.Abs(value) > maxWebMercator) ||
                coordinates[0] >= coordinates[2] || coordinates[1] >= coordinates[3])
                return false;

            normalizedBbox = string.Join(',', coordinates.Select(value => value.ToString(CultureInfo.InvariantCulture)));
            return true;
        }
    }
}
