using System.Text.Json;
using API.Core;
using API.Data;
using API.DTOs.Routes;
using API.DTOs.Routes.Requests;
using API.DTOs.Routes.Responses;
using API.Helpers;
using API.Models;
using API.Services.Caching;
using API.Services.OpenRouteService;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Routes
{
    public class RouteSuggestionService : IRouteSuggestionService
    {
        private const int PreviewPolylineMaxPoints = 25;

        private readonly ApplicationDbContext _dbContext;
        private readonly IOpenRouteServiceClient _orsClient;
        private readonly IPendingRouteCache _pendingRouteCache;

        public RouteSuggestionService(
            ApplicationDbContext dbContext,
            IOpenRouteServiceClient orsClient,
            IPendingRouteCache pendingRouteCache)
        {
            _dbContext = dbContext;
            _orsClient = orsClient;
            _pendingRouteCache = pendingRouteCache;
        }

        public async Task<Result<RouteSuggestionResponse>> SuggestAsync(Guid userId, RouteSuggestionRequest request, CancellationToken cancellationToken = default)
        {
            var coordinates = BuildCoordinateList(request);

            var orsResultResult = await GetOrsRouteAsync(
                () => _orsClient.GetCyclingRouteAsync(coordinates, cancellationToken));

            if (!orsResultResult.IsSuccess)
            {
                return Result<RouteSuggestionResponse>.Failure(orsResultResult.Error!);
            }

            return BuildPreviewResponse(userId,
                request.OriginLatitude, request.OriginLongitude,
                request.DestinationLatitude, request.DestinationLongitude,
                orsResultResult.Value!, isRoundTrip: false, requestedDistanceMeters: null);
        }

        public async Task<Result<RouteSuggestionResponse>> SuggestRoundTripAsync(Guid userId, RouteRoundTripSuggestionRequest request, CancellationToken cancellationToken = default)
        {
            var orsResultResult = await GetOrsRouteAsync(
                () => _orsClient.GetRoundTripRouteAsync(
                    request.OriginLatitude, request.OriginLongitude,
                    request.DesiredDistanceMeters,
                    cancellationToken));

            if (!orsResultResult.IsSuccess)
            {
                return Result<RouteSuggestionResponse>.Failure(orsResultResult.Error!);
            }

            // Destino = origem: é literalmente o mesmo ponto num loop.
            return BuildPreviewResponse(userId,
                request.OriginLatitude, request.OriginLongitude,
                request.OriginLatitude, request.OriginLongitude,
                orsResultResult.Value!, isRoundTrip: true, requestedDistanceMeters: request.DesiredDistanceMeters);
        }

        /// <summary>
        /// Persiste uma rota pendente. Idempotente: se este pendingRouteId já foi persistido
        /// antes (retry de rede, duplo-tap no botão guardar), devolve o Id já existente em
        /// vez de duplicar a rota na base de dados.
        /// </summary>
        public async Task<Result<Guid>> PersistPendingRouteAsync(Guid pendingRouteId, CancellationToken cancellationToken = default)
        {
            var pending = _pendingRouteCache.TryGet(pendingRouteId);
            if (pending == null)
            {
                return Result<Guid>.Failure(new Error(
                    "PENDING_ROUTE_EXPIRED", "Esta rota já não está disponível — gera uma nova sugestão.", 404));
            }

            if (pending.SuggestedRouteId is { } alreadyPersistedId)
            {
                return Result<Guid>.Success(alreadyPersistedId);
            }

            var route = BuildRoute(pending.CreatedByUserId,
                pending.OriginLatitude, pending.OriginLongitude,
                pending.DestinationLatitude, pending.DestinationLongitude,
                pending.OrsResult, pending.IsRoundTrip, pending.RequestedDistanceMeters);

            _dbContext.SuggestedRoutes.Add(route);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _pendingRouteCache.MarkPersisted(pendingRouteId, route.Id);

            return Result<Guid>.Success(route.Id);
        }

        /// <summary>
        /// Detalhe de uma rota — tenta primeiro no cache de pendentes (caso mais comum:
        /// utilizador acabou de gerar e ainda não decidiu guardar), só depois consulta a
        /// BD (rota já guardada anteriormente por qualquer utilizador).
        /// </summary>
        public async Task<Result<RouteDetailResponse>> GetRouteDetailAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var pending = _pendingRouteCache.TryGet(id);
            if (pending != null)
            {
                return Result<RouteDetailResponse>.Success(BuildDetailResponseFromOrs(id, pending.OrsResult));
            }

            var route = await _dbContext.SuggestedRoutes
                .Include(r => r.Points)
                .FirstOrDefaultAsync(r => r.Id == id, cancellationToken);

            if (route == null)
            {
                return Result<RouteDetailResponse>.Failure(new Error("ROUTE_NOT_FOUND", "Rota não foi encontrada.", 404));
            }

            var orderedPoints = route.Points.OrderBy(p => p.Sequence).ToList();
            var elevation = RouteElevationCalculator.BuildSummary(orderedPoints);
            var polyline = orderedPoints
                .Select(p => new RouteDetailPointResponse(p.Latitude, p.Longitude))
                .ToList();

            var response = new RouteDetailResponse(
                route.Id,
                route.DistanceMeters,
                elevation.Gain,
                elevation.Loss,
                elevation.Net,
                polyline,
                elevation.Profile,
                RouteDifficultyClassifier.Classify(elevation.Gain, route.DistanceMeters));

            return Result<RouteDetailResponse>.Success(response);
        }

        // ⚠️ Ver nota no final da resposta — depende de RouteElevationCalculator.BuildSummary
        // aceitar List<ORSRoutePoint>, não só List<RoutePoint>.
        private static RouteDetailResponse BuildDetailResponseFromOrs(Guid id, ORSRouteResult orsResult)
        {
            var elevation = RouteElevationCalculator.BuildSummary(orsResult.Points);
            var polyline = orsResult.Points
                .Select(p => new RouteDetailPointResponse(p.Latitude, p.Longitude))
                .ToList();

            return new RouteDetailResponse(
                id,
                orsResult.DistanceMeters,
                elevation.Gain,
                elevation.Loss,
                elevation.Net,
                polyline,
                elevation.Profile,
                RouteDifficultyClassifier.Classify(elevation.Gain, orsResult.DistanceMeters));
        }

        private static async Task<Result<ORSRouteResult>> GetOrsRouteAsync(Func<Task<ORSRouteResult>> orsCall)
        {
            try
            {
                var result = await orsCall();
                return Result<ORSRouteResult>.Success(result);
            }
            catch (Exception ex)
            {
                return Result<ORSRouteResult>.Failure(new Error(
                    "ROUTE_PROVIDER_ERROR", $"Não foi possível calcular a rota: {ex.Message}", 502));
            }
        }

        private static List<ORSCoordinate> BuildCoordinateList(RouteSuggestionRequest request)
        {
            var coordinates = new List<ORSCoordinate>
            {
                new(request.OriginLatitude, request.OriginLongitude)
            };

            if (request.Waypoints is { Count: > 0 })
            {
                coordinates.AddRange(request.Waypoints.Select(w => new ORSCoordinate(w.Latitude, w.Longitude)));
            }

            coordinates.Add(new ORSCoordinate(request.DestinationLatitude, request.DestinationLongitude));
            return coordinates;
        }

        private static string BuildPreviewPolylineJson(List<ORSRoutePoint> points, int maxPoints = PreviewPolylineMaxPoints)
        {
            var sampled = points.Count <= maxPoints
                ? points
                : points.Where((_, i) => i % (int)Math.Ceiling(points.Count / (double)maxPoints) == 0).ToList();

            var pairs = sampled.Select(p => new[] { p.Latitude, p.Longitude }).ToList();
            return JsonSerializer.Serialize(pairs);
        }

        // Não persiste nada — só monta a resposta de preview e guarda o resultado da ORS
        // em cache para uso posterior (caso o utilizador guarde a rota).
        private Result<RouteSuggestionResponse> BuildPreviewResponse(
            Guid userId, double originLat, double originLon, double destLat, double destLon,
            ORSRouteResult orsResult, bool isRoundTrip, double? requestedDistanceMeters)
        {
            var elevationGain = RouteElevationCalculator.CalculateElevationGainFromOrs(orsResult.Points);

            var pending = new PendingRoute(
                userId, originLat, originLon, destLat, destLon,
                isRoundTrip, requestedDistanceMeters, orsResult);

            var pendingRouteId = _pendingRouteCache.Store(pending);

            var response = new RouteSuggestionResponse(
                pendingRouteId,
                orsResult.DistanceMeters,
                elevationGain,
                orsResult.Points.Select((p, i) => new RoutePointResponse(p.Latitude, p.Longitude, p.AltitudeMeters, i)).ToList(),
                RouteDifficultyClassifier.Classify(elevationGain, orsResult.DistanceMeters)
            );

            return Result<RouteSuggestionResponse>.Success(response);
        }

        private static SuggestedRoute BuildRoute(
            Guid createdByUserId, double originLat, double originLon, double destLat, double destLon,
            ORSRouteResult orsResult, bool isRoundTrip, double? requestedDistanceMeters)
        {
            var simplifiedPoints = RoutePolylineSimplifier.Simplify(
                orsResult.Points,
                getLatitude: p => p.Latitude,
                getLongitude: p => p.Longitude);

            var route = new SuggestedRoute
            {
                Id = Guid.NewGuid(),
                CreatedByUserId = createdByUserId,
                OriginLatitude = originLat,
                OriginLongitude = originLon,
                DestinationLatitude = destLat,
                DestinationLongitude = destLon,
                DistanceMeters = orsResult.DistanceMeters,
                ElevationGainMeters = RouteElevationCalculator.CalculateElevationGainFromOrs(orsResult.Points),
                Source = "openrouteservice",
                IsRoundTrip = isRoundTrip,
                RequestedDistanceMeters = requestedDistanceMeters,
                PreviewPolylineJson = BuildPreviewPolylineJson(orsResult.Points),
                CreatedAt = DateTime.UtcNow
            };

            for (int i = 0; i < simplifiedPoints.Count; i++)
            {
                var p = simplifiedPoints[i];
                route.Points.Add(new RoutePoint
                {
                    SuggestedRouteId = route.Id,
                    Sequence = i,
                    Latitude = p.Latitude,
                    Longitude = p.Longitude,
                    AltitudeMeters = p.AltitudeMeters
                });
            }

            return route;
        }
    }
}