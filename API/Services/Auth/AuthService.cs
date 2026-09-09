using API.Core;
using API.Data;
using API.DTOs;
using API.Models;
using API.Services.RefreshTokens;
using API.Services.Token;
using FluentValidation;
using LIB.Auth;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace API.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly ITokenService _tokenService;
        private readonly IRefreshTokenService _refreshTokenService;
        private readonly IValidator<RegisterRequest> _registerValidator;
        private readonly IValidator<LoginRequest> _loginValidator;
        private readonly PasswordHasher<User> _passwordHasher = new();

        public AuthService(ApplicationDbContext dbContext, ITokenService tokenService, IRefreshTokenService refreshTokenService, IValidator<RegisterRequest> registerValidator, IValidator<LoginRequest> loginValidator)
        {
            _dbContext = dbContext;
            _tokenService = tokenService;
            _refreshTokenService = refreshTokenService;
            _registerValidator = registerValidator;
            _loginValidator = loginValidator;
        }

        public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request, CancellationToken cancellationToken = default)
        {
            // Validar dados
            var result = await _registerValidator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                return Result<AuthResponse>.Failure(new Error(
                    "VALIDATION_ERROR",
                     result.Errors[0].ErrorMessage,
                     400
                    ));
            }

            // Verificar se existe email repetido
            bool emailExists = await _dbContext.Users
                .AnyAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

            if (emailExists)
            {
                return Result<AuthResponse>.Failure(
                    new Error("AUTH_EMAIL_EXISTS", "Email ja se encontra registado.", 400));
            }

            // Criar utilizador
            User user = new User
            {
                Id = Guid.NewGuid(),
                Name = request.Name,
                // Normalizar emails na DB
                Email = request.Email.ToLowerInvariant(),
                CreatedAt = DateTime.UtcNow,
            };

            // Adicionar password hasheada
            user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);

            // Adicionar a db
            _dbContext.Users.Add(user);
            try
            {
                await _dbContext.SaveChangesAsync(cancellationToken);
            }
            catch (DbUpdateException exception) when (exception.InnerException is PostgresException { SqlState: PostgresErrorCodes.UniqueViolation })
            {
                return Result<AuthResponse>.Failure(
                    new Error("AUTH_EMAIL_EXISTS", "Email ja se encontra registado.", 409));
            }

            // Gerar tokens e retornar
            return Result<AuthResponse>.Success(await GenerateTokens(user, cancellationToken));
        }


        public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request, CancellationToken cancellationToken = default)
        {

            // Validar dados
            var result = await _loginValidator.ValidateAsync(request, cancellationToken);
            if (!result.IsValid)
            {
                return Result<AuthResponse>.Failure(new Error(
                    "VALIDATION_ERROR",
                     result.Errors[0].ErrorMessage,
                     400
                    ));
            }

            // Obter usuario e verificar password
            User? user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Email == request.Email.ToLowerInvariant(), cancellationToken);

            if (user is null)
            {
                return Result<AuthResponse>.Failure(
                    new Error("AUTH_INVALID_CREDENTIALS",
                    "Email ou Password incorretos.",
                    401));
            }

            // VERIFICAR PASSWORD

            PasswordVerificationResult verificationResult = _passwordHasher.VerifyHashedPassword(user, user.PasswordHash!, request.Password);

            if (verificationResult == PasswordVerificationResult.Failed)
            {
                return Result<AuthResponse>.Failure(
                    new Error("AUTH_INVALID_CREDENTIALS", "Email ou Password incorretos.", 401));
            }
            // VERIFICAR SE E PRECISO NOVO HASH
            else if (verificationResult == PasswordVerificationResult.SuccessRehashNeeded)
            {
                user.PasswordHash = _passwordHasher.HashPassword(user, request.Password);
                await _dbContext.SaveChangesAsync(cancellationToken);
            }

            // Gerar tokens e retornar
            return Result<AuthResponse>.Success(
                await GenerateTokens(user, cancellationToken)
            );
        }

        public async Task<AuthResponse> GenerateTokens(User user, CancellationToken cancellationToken = default)
        {
            string accessToken = _tokenService.GenerateAccessToken(user);
            string refreshToken = await _refreshTokenService.CreateAsync(user.Id, null, cancellationToken);

            return new(
               AccessToken: accessToken,
               RefreshToken: refreshToken,
               ExpiresAt: DateTime.UtcNow.AddMinutes(15));

        }

    }
}
