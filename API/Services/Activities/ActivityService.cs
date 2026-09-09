using API.Core;
using API.Data;
using API.DTOs;
using API.DTOs.Actitivities.Requests;
using API.DTOs.Actitivities.Responses;
using API.Models;
using Microsoft.EntityFrameworkCore;

namespace API.Services.Activities
{
    public class ActivityService : IActivityService
    {
        private readonly ApplicationDbContext _dbContext;

        public ActivityService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static ActivityResponse ToResponse(Activity activity) => new(
            activity.Id,
            activity.StartedAt,
            activity.EndedAt,
            activity.DistanceMeters,
            activity.TotalTimeSeconds,
            activity.MovingTimeSeconds,
            activity.AverageSpeedKmh,
            activity.MaxSpeedKmh,
            activity.ElevationGainMeters
        );

        public async Task<Result<ActivityResponse>> CreateAsync(Guid userId, CreateActivityRequest request, CancellationToken cancellationToken = default)
        {
            // Idempotência: se já existe uma atividade com este Id (reenvio de sync, retry de rede), devolve a existente sem duplicar
            var existing = await _dbContext.Activities
                .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

            if (existing is not null)
            {
                if (existing.UserId != userId)
                {
                    return Result<ActivityResponse>.Failure(new Error(
                        "ACTIVITY_ID_CONFLICT", "Este identificador de atividade já pertence a outro utilizador.", 409));
                }

                return Result<ActivityResponse>.Success(ToResponse(existing));
            }

            var activity = new Activity
            {
                Id = request.Id,
                UserId = userId,
                StartedAt = request.StartedAt,
                EndedAt = request.EndedAt,
                DistanceMeters = request.DistanceMeters,
                TotalTimeSeconds = request.TotalTimeSeconds,
                MovingTimeSeconds = request.MovingTimeSeconds,
                AverageSpeedKmh = request.AverageSpeedKmh,
                MaxSpeedKmh = request.MaxSpeedKmh,
                ElevationGainMeters = request.ElevationGainMeters,
                CreatedAt = DateTime.UtcNow
            };

            var track = new Track { Id = Guid.NewGuid(), ActivityId = activity.Id };

            foreach (var segmentRequest in request.Segments)
            {
                var segment = new TrackSegment
                {
                    Id = Guid.NewGuid(),
                    TrackId = track.Id,
                    SequenceIndex = segmentRequest.SequenceIndex,
                    StartedAt = segmentRequest.StartedAt,
                    EndedAt = segmentRequest.EndedAt
                };

                foreach (var pointRequest in segmentRequest.Points)
                {
                    segment.Points.Add(new TrackPoint
                    {
                        TrackSegmentId = segment.Id,
                        Sequence = pointRequest.Sequence,
                        Latitude = pointRequest.Latitude,
                        Longitude = pointRequest.Longitude,
                        AltitudeMeters = pointRequest.AltitudeMeters,
                        RecordedAt = pointRequest.RecordedAt
                    });
                }

                track.Segments.Add(segment);
            }

            activity.Track = track;

            _dbContext.Activities.Add(activity);
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException)
            {
                _dbContext.ChangeTracker.Clear();
                var concurrent = await _dbContext.Activities
                    .AsNoTracking()
                    .FirstOrDefaultAsync(a => a.Id == request.Id, cancellationToken);

                if (concurrent is not null && concurrent.UserId == userId)
                    return Result<ActivityResponse>.Success(ToResponse(concurrent));

                if (concurrent is not null)
                    return Result<ActivityResponse>.Failure(new Error(
                        "ACTIVITY_ID_CONFLICT", "Este identificador de atividade já pertence a outro utilizador.", 409));

                throw;
            }

            return Result<ActivityResponse>.Success(ToResponse(activity));
        }

        public async Task<Result<List<ActivityResponse>>> GetByUserAsync(Guid userId, int? days, CancellationToken cancellationToken = default)
        {
            var query = _dbContext.Activities.Where(a => a.UserId == userId);

            if (days.HasValue)
            {
                var since = DateTime.UtcNow.AddDays(-days.Value);
                query = query.Where(a => a.StartedAt >= since);
            }

            var activities = await query
                .OrderByDescending(a => a.StartedAt)
                .Select(a => ToResponse(a))
                .ToListAsync(cancellationToken);


            return Result<List<ActivityResponse>>.Success(activities);
        }

        public async Task<Result<ActivityDetailResponse>> GetDetailByIdAsync(Guid userId, Guid activityId, CancellationToken cancellationToken = default)
        {
            var activity = await _dbContext.Activities
                .Include(a => a.Track)
                    .ThenInclude(t => t.Segments)
                        .ThenInclude(s => s.Points)
                .FirstOrDefaultAsync(a => a.Id == activityId && a.UserId == userId, cancellationToken);

            if (activity is null)
            {
                return Result<ActivityDetailResponse>.Failure(new Error(
                    "ACTIVITY_NOT_FOUND", "A atividade não foi encontrada.", 404));
            }

            var points = activity.Track.Segments
                .OrderBy(s => s.SequenceIndex)
                .SelectMany(s => s.Points
                    .OrderBy(p => p.Sequence)
                    .Select(p => new TrackPointResponse(p.Latitude, p.Longitude, p.AltitudeMeters, p.RecordedAt, s.SequenceIndex)))
                .ToList();

            var response = new ActivityDetailResponse(
                activity.Id,
                activity.StartedAt,
                activity.EndedAt,
                activity.DistanceMeters,
                activity.TotalTimeSeconds,
                activity.MovingTimeSeconds,
                activity.AverageSpeedKmh,
                activity.MaxSpeedKmh,
                activity.ElevationGainMeters,
                points
            );

            return Result<ActivityDetailResponse>.Success(response);
        }
        public async Task<Result> DeleteAsync(Guid userId, Guid activityId, CancellationToken cancellationToken = default)
        {
            var activity = await _dbContext.Activities
                .FirstOrDefaultAsync(a => a.Id == activityId && a.UserId == userId, cancellationToken);

            // Idempotente: não encontrada (já apagada, ou nunca chegou a sincronizar) = objetivo já cumprido.
            // Não distinguir "não existe" de "não é tua" evita enumeração de IDs de outros utilizadores.
            if (activity is null)
                return Result.Success();

            _dbContext.Activities.Remove(activity); // cascade remove Track → TrackSegments → TrackPoints
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result.Success();
        }
    }
}
