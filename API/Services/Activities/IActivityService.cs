using API.Core;
using API.DTOs;
using API.DTOs.Actitivities.Requests;
using API.DTOs.Actitivities.Responses;

namespace API.Services.Activities
{
    public interface IActivityService
    {
        Task<Result<ActivityResponse>> CreateAsync(Guid userId, CreateActivityRequest request, CancellationToken cancellationToken);
        Task<Result<List<ActivityResponse>>> GetByUserAsync(Guid userId, int? days, CancellationToken cancellationToken);
        Task<Result<ActivityDetailResponse>> GetDetailByIdAsync(Guid userId, Guid activityId, CancellationToken cancellationToken);
        Task<Result> DeleteAsync(Guid userId, Guid activityId, CancellationToken cancellationToken = default);
    }
}
