
using API.Core;
using LIB.Auth;

namespace API.Services.Users
{
    public interface IUserService
    {
        public Task<Result<UserResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken);
    }
}
