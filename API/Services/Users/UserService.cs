using API.Core;
using API.Data;
using API.Models;
using LIB.Auth;

namespace API.Services.Users
{
    public class UserService : IUserService
    {
        private readonly ApplicationDbContext _dbContext;

        public UserService(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<UserResponse>> GetUserByIdAsync(Guid userId, CancellationToken cancellationToken)
        {
            User? user = await _dbContext.Users
                .FindAsync(userId, cancellationToken);

            if (user == null)
            {
                return Result<UserResponse>.Failure(
                    new Error(
                        "USER_NOT_FOUND",
                        "Utilizador não foi encontrado.",
                        404));
            }

            UserResponse response = new UserResponse
            (
                user.Id,
                user.Name,
                user.Email
            );

            return Result<UserResponse>.Success(response);
        }
    }
}
