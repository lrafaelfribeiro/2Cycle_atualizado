using API.Core;
using API.Data;
using API.Models;
using API.Services.Token;
using LIB.Auth;
using Microsoft.EntityFrameworkCore;

namespace API.Services.RefreshTokens
{
    public class RefreshTokenService : IRefreshTokenService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;

        public RefreshTokenService(ApplicationDbContext dbContext, ITokenService tokenService)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
        }

        public async Task<string> CreateAsync(Guid userId, Guid? familyId = null, CancellationToken cancellationToken = default)
        {
            // gerar o token
            string rawToken = _tokenService.GenerateRefreshToken();

            RefreshToken refreshToken = new RefreshToken
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                FamilyId = familyId ?? Guid.NewGuid(),
                HashToken = _tokenService.HashRefreshToken(rawToken), // hashear o token
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.AddDays(30),
                Revoked = false
            };

            _dbContext.RefreshTokens.Add(refreshToken);
            await _dbContext.SaveChangesAsync(cancellationToken);

            return rawToken;
        }

        public async Task<Result<AuthResponse>> RotateAsync(string tokenValue, CancellationToken cancellationToken = default)
        {
            // Hashear e procurar na db
            string hashedToken = _tokenService.HashRefreshToken(tokenValue);
            RefreshToken? oldToken = await _dbContext.RefreshTokens
                .AsNoTracking()
                .Include(rt => rt.User)
                .FirstOrDefaultAsync(rt => rt.HashToken == hashedToken, cancellationToken);

            // Verificar se foi encontrado
            if (oldToken == null)
            {
                return Result<AuthResponse>.Failure(new Error
                (
                    "INVALID_TOKEN",
                    "Token Invalido",
                    401
                ));
            }

            // Verificar se e um token que ja foi revogado
            if (oldToken.Revoked)
            {
                await RevokeFamilyAsync(oldToken.FamilyId, cancellationToken);
                return Result<AuthResponse>.Failure(new Error
                (
                    "REVOKED_TOKEN",
                    "Token ja foi revogado",
                    401
                ));
            }

            // Verificar se ja expirou
            if (oldToken.ExpiresAt < DateTime.UtcNow)
            {
                return Result<AuthResponse>.Failure(new Error
                (
                    "EXPIRED_TOKEN",
                    "Token expirado",
                    401
                ));
            }

            // Revogar o token condicionalmente. Só o primeiro pedido concorrente pode continuar.
            var revoked = await _dbContext.RefreshTokens
                .Where(rt => rt.Id == oldToken.Id && !rt.Revoked && rt.ExpiresAt >= DateTime.UtcNow)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.Revoked, true), cancellationToken);

            if (revoked != 1)
            {
                return Result<AuthResponse>.Failure(new Error(
                    "REVOKED_TOKEN", "Token ja foi revogado", 401));
            }

            // Criar novos tokens
            string newAccessToken = _tokenService.GenerateAccessToken(oldToken.User);
            string newRefreshToken = await CreateAsync(oldToken.UserId, oldToken.FamilyId, cancellationToken);

            return Result<AuthResponse>.Success(new AuthResponse(newAccessToken, newRefreshToken, DateTime.UtcNow.AddMinutes(15)));
        }

        private async Task RevokeFamilyAsync(Guid familyId, CancellationToken cancellationToken)
        {
            await _dbContext.RefreshTokens
                .Where(rt => rt.FamilyId == familyId && !rt.Revoked)
                .ExecuteUpdateAsync(setters => setters.SetProperty(rt => rt.Revoked, true), cancellationToken);
        }

        public async Task RevokeAsync(string tokenValue, string userId, CancellationToken cancellationToken)
        {
            string hashedToken = _tokenService.HashRefreshToken(tokenValue);

            RefreshToken? refreshToken = await _dbContext.RefreshTokens
                .FirstOrDefaultAsync(rt => rt.HashToken == hashedToken, cancellationToken);

            if (refreshToken == null || refreshToken.UserId.ToString() != userId)
            {
                throw new UnauthorizedAccessException();
            }
            else if (!refreshToken.Revoked)
            {
                refreshToken.Revoked = true;
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
        }
    }
}
