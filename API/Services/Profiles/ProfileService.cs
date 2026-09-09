using API.Core;
using API.Data;
using API.DTOs;
using API.Models;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Services.Profiles
{
    public class ProfileService : IProfileService
    {
        private readonly ApplicationDbContext _dbContext;

        public ProfileService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        private static double? ComputeBmi(double heightCm, double? weightKg)
        {
            if (weightKg is null || heightCm <= 0)
                return null;

            double heightM = heightCm / 100.0;
            return Math.Round(weightKg.Value / (heightM * heightM), 1);
        }

        private async Task<double?> GetCurrentWeightAsync(Guid userId, CancellationToken cancellationToken)
        {
            return await _dbContext.WeightLogs
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.RecordedAt)
                .Select(w => (double?)w.WeightKg)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<Result<ProfileResponse>> GetProfileByIdAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var profile = await _dbContext.Profiles
                .Include(p => p.User)
                .FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null)
            {
                return Result<ProfileResponse>.Failure(new Error(
                    "PROFILE_NOT_FOUND", "O perfil não foi encontrado.", 404));
            }

            double? currentWeight = await GetCurrentWeightAsync(userId, cancellationToken);

            return Result<ProfileResponse>.Success(new ProfileResponse(
                profile.User.Name,
                profile.BirthDate,
                profile.HeightCm,
                currentWeight,
                profile.TargetWeightKg,
                profile.ActivityDaysPerWeek,
                profile.Sex,
                ComputeBmi(profile.HeightCm, currentWeight)
            ));
        }

        public async Task<Result<ProfileResponse>> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken = default)
        {
            bool exists = await _dbContext.Profiles.AnyAsync(p => p.UserId == userId, cancellationToken);
            if (exists)
            {
                return Result<ProfileResponse>.Failure(new Error(
                    "PROFILE_ALREADY_EXISTS", "O perfil já existe.", 409));
            }

            var profile = new Profile
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                BirthDate = request.BirthDate,
                HeightCm = request.HeightCm,
                TargetWeightKg = request.TargetWeightKg,
                ActivityDaysPerWeek = request.ActivityDaysPerWeek,
                Sex = request.Sex,
                CreatedAt = DateTime.UtcNow,
                LastUpdatedAt = DateTime.UtcNow
            };

            var initialWeightLog = new WeightLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeightKg = request.InitialWeightKg,
                RecordedAt = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.Profiles.Add(profile);
            _dbContext.WeightLogs.Add(initialWeightLog);
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Result<ProfileResponse>.Failure(new Error(
                    "PROFILE_ALREADY_EXISTS", "O perfil já existe.", 409));
            }

            return await GetProfileByIdAsync(userId, cancellationToken);
        }

        public async Task<Result<ProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken = default)
        {
            var profile = await _dbContext.Profiles.FirstOrDefaultAsync(p => p.UserId == userId, cancellationToken);

            if (profile is null)
            {
                return Result<ProfileResponse>.Failure(new Error(
                    "PROFILE_NOT_FOUND", "O perfil não foi encontrado.", 404));
            }

            profile.BirthDate = request.BirthDate;
            profile.HeightCm = request.HeightCm;
            profile.TargetWeightKg = request.TargetWeightKg;
            profile.ActivityDaysPerWeek = request.ActivityDaysPerWeek;
            profile.Sex = request.Sex;
            profile.LastUpdatedAt = DateTime.UtcNow;

            await _dbContext.SaveChangesAsync(cancellationToken);

            return await GetProfileByIdAsync(userId, cancellationToken);
        }

        public async Task<Result<WeightLogResponse>> AddWeightLogAsync(Guid userId, AddWeightLogRequest request, CancellationToken cancellationToken = default)
        {
            bool profileExists = await _dbContext.Profiles.AnyAsync(p => p.UserId == userId, cancellationToken);
            if (!profileExists)
            {
                return Result<WeightLogResponse>.Failure(new Error(
                    "PROFILE_NOT_FOUND", "O perfil não foi encontrado.", 404));
            }

            var weightLog = new WeightLog
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                WeightKg = request.WeightKg,
                RecordedAt = request.RecordedAt ?? DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow
            };

            _dbContext.WeightLogs.Add(weightLog);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return Result<WeightLogResponse>.Success(new WeightLogResponse(weightLog.WeightKg, weightLog.RecordedAt));
        }

        public async Task<Result<List<WeightLogResponse>>> GetWeightHistoryAsync(Guid userId, CancellationToken cancellationToken = default)
        {
            var history = await _dbContext.WeightLogs
                .Where(w => w.UserId == userId)
                .OrderByDescending(w => w.RecordedAt)
                .Select(w => new WeightLogResponse(w.WeightKg, w.RecordedAt))
                .ToListAsync(cancellationToken);

            return Result<List<WeightLogResponse>>.Success(history);
        }
    }
}
