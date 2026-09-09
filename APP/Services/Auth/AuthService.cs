using APP.Core;
using APP.DTOs;
using APP.Models;
using APP.Services.Token;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace APP.Services.Auth
{
    public class AuthService : IAuthService
    {
        private readonly HttpClient _httpClient;
        private readonly ITokenStorageService _tokenStorageService;
        private readonly ILogger<AuthService> _logger;

        public AuthService(HttpClient httpClient, ILogger<AuthService> logger, ITokenStorageService tokenStorageService)
        {
            _httpClient = httpClient;
            _logger = logger;
            _tokenStorageService = tokenStorageService;
        }

        private async Task<Result<AuthResponseDTO>> PostAuthRequestAsync(
     string endpoint, object request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AuthResponseDTO>(cancellationToken);

                    if (result is null)
                        return Result<AuthResponseDTO>.Failure(new Error(
                            "DESERIALIZATION_ERROR", "Resposta inválida do servidor.", 500));

                    await _tokenStorageService.SaveTokensAsync(result.accessToken, result.refreshToken, result.expiresAt);
                    return Result<AuthResponseDTO>.Success(result);
                }

                var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDTO>(cancellationToken);

                if (problem is null)
                    return Result<AuthResponseDTO>.Failure(new Error(
                        "UNKNOWN_ERROR", "Erro desconhecido do servidor.", (int)response.StatusCode));

                var code = problem.Type?.Split(":").LastOrDefault() ?? "UNKNOWN_ERROR";
                return Result<AuthResponseDTO>.Failure(new Error(code, problem.Title, (int)response.StatusCode));
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede em {Endpoint}", endpoint);
                return Result<AuthResponseDTO>.Failure(new Error("NETWORK_ERROR", "Sem ligação à internet", 0));
            }
            catch (TaskCanceledException)
            {
                return Result<AuthResponseDTO>.Failure(new Error(
                    "TIMEOUT", "O pedido demorou demasiado tempo.", 0));
            }
        }

        public Task<Result<AuthResponseDTO>> LoginAsync(string email, string password, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                Email = email,
                Password = password
            };
            return PostAuthRequestAsync("/api/auth/login", request, cancellationToken);
        }

        public Task<Result<AuthResponseDTO>> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken = default)
        {
            var request = new
            {
                Name = name,
                Email = email,
                Password = password
            };
            return PostAuthRequestAsync("/api/auth/register", request, cancellationToken);
        }

        public Task<Result<AuthResponseDTO>> RefreshTokensAsync(string refreshToken, CancellationToken cancellationToken)
        {
            var request = new
            {
                RefreshToken = refreshToken
            };

            return PostAuthRequestAsync("/api/auth/refresh", request, cancellationToken);
        }
    }
}