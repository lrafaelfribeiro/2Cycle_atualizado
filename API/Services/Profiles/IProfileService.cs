using API.Core;
using API.DTOs;

namespace API.Services.Profiles
{
    public interface IProfileService
    {
        Task<Result<ProfileResponse>> GetProfileByIdAsync(Guid userId, CancellationToken cancellationToken);
        Task<Result<ProfileResponse>> CreateProfileAsync(Guid userId, CreateProfileRequest request, CancellationToken cancellationToken);
        Task<Result<ProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request, CancellationToken cancellationToken);
        Task<Result<WeightLogResponse>> AddWeightLogAsync(Guid userId, AddWeightLogRequest request, CancellationToken cancellationToken);
        Task<Result<List<WeightLogResponse>>> GetWeightHistoryAsync(Guid userId, CancellationToken cancellationToken);
    }
}
