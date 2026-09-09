using API.Core;
using API.Data;
using API.DTOs.Routes;
using API.DTOs.Routes.Requests;
using API.DTOs.Routes.Responses;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Ocsp;
using System.Text.Json;

namespace API.Services.Routes
{
    public class SavedRouteService : ISavedRouteService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IRouteSuggestionService _routeSuggestionService;

        public SavedRouteService(ApplicationDbContext dbContext, IRouteSuggestionService routeSuggestionService)
        {
            _dbContext = dbContext;
            _routeSuggestionService = routeSuggestionService;
        }

        /// <summary>
        /// Guarda uma rota sugerida. "pendingRouteId" é o Id devolvido pelo endpoint de
        /// sugestão — a rota só é persistida em SuggestedRoutes agora, não antes.
        /// </summary>
        public async Task<Result> SaveAsync(Guid userId, Guid pendingRouteId, SaveRouteRequest request, CancellationToken cancellationToken = default)
        {
            var persistResult = await _routeSuggestionService.PersistPendingRouteAsync(pendingRouteId, cancellationToken);
            if (!persistResult.IsSuccess)
            {
                return Result.Failure(persistResult.Error!);
            }

            var suggestedRouteId = persistResult.Value!;

            var existing = await _dbContext.UserSuggestedRoutes
                .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.SuggestedRouteId == suggestedRouteId, cancellationToken);

            if (existing != null)
            {
                if (!string.IsNullOrWhiteSpace(request.RouteName))
                {
                    existing.Name = request.RouteName;
                }
            }
            else
            {
                _dbContext.UserSuggestedRoutes.Add(new UserSuggestedRoute
                {
                    UserId = userId,
                    SuggestedRouteId = suggestedRouteId,
                    IsFavorite = false,
                    SavedAt = DateTime.UtcNow,
                    Name = request.RouteName
                });
            }

            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> UnsaveAsync(Guid userId, Guid suggestedRouteId, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.UserSuggestedRoutes
                .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.SuggestedRouteId == suggestedRouteId, cancellationToken);

            if (existing == null)
            {
                return Result.Failure(new Error("SAVED_ROUTE_NOT_FOUND", "Esta rota não está guardada.", 404));
            }

            _dbContext.UserSuggestedRoutes.Remove(existing);
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result> SetFavoriteAsync(Guid userId, Guid suggestedRouteId, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var existing = await _dbContext.UserSuggestedRoutes
                .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.SuggestedRouteId == suggestedRouteId, cancellationToken);

            if (existing == null)
            {
                // Regra de domínio: só se pode marcar como favorita uma rota já guardada.
                return Result.Failure(new Error("SAVED_ROUTE_NOT_FOUND", "Esta rota não está guardada.", 404));
            }

            existing.IsFavorite = isFavorite;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }

        public async Task<Result<List<SavedRouteSummaryResponse>>> GetSavedAsync(Guid userId, bool favoritesOnly, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.UserSuggestedRoutes
                .Where(usr => usr.UserId == userId)
                .Include(usr => usr.SuggestedRoute)
                .AsQueryable();

            if (favoritesOnly)
            {
                query = query.Where(usr => usr.IsFavorite);
            }

            var rows = await query
                .OrderByDescending(usr => usr.SavedAt)
                .Select(usr => new
                {
                    usr.Id,
                    usr.SuggestedRouteId,
                    usr.Name,
                    usr.SuggestedRoute.DistanceMeters,
                    usr.SuggestedRoute.ElevationGainMeters,
                    usr.SuggestedRoute.IsRoundTrip,
                    usr.IsFavorite,
                    usr.SavedAt,
                    usr.SuggestedRoute.PreviewPolylineJson
                })
                .ToListAsync(cancellationToken);

            var saved = rows.Select(r => new SavedRouteSummaryResponse(
                r.Id,
                r.SuggestedRouteId,
                r.Name ?? string.Empty, // Name é nullable na entidade (rota pode não ter sido nomeada); ver nota abaixo
                r.DistanceMeters,
                r.ElevationGainMeters,
                r.IsFavorite,
                r.SavedAt,
                DeserializePreviewPolyline(r.PreviewPolylineJson),
                RouteDifficultyClassifier.Classify(r.ElevationGainMeters, r.DistanceMeters)
            )).ToList();

            return Result<List<SavedRouteSummaryResponse>>.Success(saved);
        }

        /// <summary>
        /// Detalhe completo de UMA rota guardada por ESTE utilizador, identificada por suggestedRouteId
        /// (nunca pelo Id interno de UserSuggestedRoute — isso ficava só na resposta de GetSavedAsync
        /// para referência, nunca como parâmetro de rota).
        /// </summary>
        public async Task<Result<SavedRouteDetailResponse>> GetSavedRouteDetailAsync(Guid userId, Guid suggestedRouteId, CancellationToken cancellationToken = default)
        {
            var savedRoute = await _dbContext.UserSuggestedRoutes
                .Include(usr => usr.SuggestedRoute)
                    .ThenInclude(sr => sr.Points)
                .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.SuggestedRouteId == suggestedRouteId, cancellationToken);

            if (savedRoute == null)
            {
                return Result<SavedRouteDetailResponse>.Failure(new Error("SAVED_ROUTE_NOT_FOUND", "Rota guardada não encontrada.", 404));
            }

            var orderedPoints = savedRoute.SuggestedRoute.Points.OrderBy(p => p.Sequence).ToList();
            var elevation = RouteElevationCalculator.BuildSummary(orderedPoints);
            var polyline = orderedPoints
                .Select(p => new RouteDetailPointResponse(p.Latitude, p.Longitude))
                .ToList();

            var response = new SavedRouteDetailResponse(
                savedRoute.Id,
                savedRoute.SuggestedRouteId,
                savedRoute.Name,
                savedRoute.SuggestedRoute.DistanceMeters,
                elevation.Gain,
                elevation.Loss,
                elevation.Net,
                savedRoute.IsFavorite,
                savedRoute.SavedAt,
                polyline,
                elevation.Profile,
                RouteDifficultyClassifier.Classify(elevation.Gain, savedRoute.SuggestedRoute.DistanceMeters));

            return Result<SavedRouteDetailResponse>.Success(response);
        }

        private static List<PreviewPointResponse> DeserializePreviewPolyline(string? previewPolylineJson)
        {
            var points = JsonSerializer.Deserialize<List<double[]>>(previewPolylineJson ?? "[]") ?? [];
            return points.Select(p => new PreviewPointResponse(p[0], p[1])).ToList();
        }
        public async Task<Result> RenameAsync(Guid userId, Guid suggestedRouteId, string routeName, CancellationToken cancellationToken = default)
        {
            var savedRoute = await _dbContext.UserSuggestedRoutes
                .FirstOrDefaultAsync(usr => usr.UserId == userId && usr.SuggestedRouteId == suggestedRouteId, cancellationToken);

            if (savedRoute == null)
            {
                return Result.Failure(new Error("SAVED_ROUTE_NOT_FOUND", "Rota guardada não encontrada.", 404));
            }

            savedRoute.Name = routeName;
            await _dbContext.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
    }
}
