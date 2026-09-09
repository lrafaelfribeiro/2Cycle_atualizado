using APP.Core;
using APP.DTOs;
using APP.DTOs.Profile;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace APP.Services.Profile
{
    public class ProfileService : IProfileService
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<ProfileService> _logger;

        public ProfileService(HttpClient httpClient, ILogger<ProfileService> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<Result<ProfileResponseDTO>> CreateProfileAsync(CreateProfileRequestDTO request, CancellationToken cancellationToken)
        {
            return await PostAsync("api/profiles/me", request, cancellationToken);
        }

        public async Task<Result<ProfileResponseDTO>> GetMyProfileAsync(CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.GetAsync("api/profiles/me", cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ProfileResponseDTO>(cancellationToken);
                    return result is null
                        ? Result<ProfileResponseDTO>.Failure(new Error("DESERIALIZATION_ERROR", "Resposta inválida do servidor.", 500))
                        : Result<ProfileResponseDTO>.Success(result);
                }

                return await ReadErrorAsync(response, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede em GET api/profiles/me");
                return Result<ProfileResponseDTO>.Failure(new Error("NETWORK_ERROR", "Sem ligação à internet", 0));
            }
        }

        private async Task<Result<ProfileResponseDTO>> PostAsync(string endpoint, object request, CancellationToken cancellationToken)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync(endpoint, request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<ProfileResponseDTO>(cancellationToken);
                    return result is null
                        ? Result<ProfileResponseDTO>.Failure(new Error("DESERIALIZATION_ERROR", "Resposta inválida do servidor.", 500))
                        : Result<ProfileResponseDTO>.Success(result);
                }

                return await ReadErrorAsync(response, cancellationToken);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Falha de rede em {Endpoint}", endpoint);
                return Result<ProfileResponseDTO>.Failure(new Error("NETWORK_ERROR", "Sem ligação à internet", 0));
            }
            catch (TaskCanceledException)
            {
                return Result<ProfileResponseDTO>.Failure(new Error("TIMEOUT", "O pedido demorou demasiado tempo.", 0));
            }
        }

        private static async Task<Result<ProfileResponseDTO>> ReadErrorAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDTO>(cancellationToken);

            if (problem is null)
                return Result<ProfileResponseDTO>.Failure(new Error("UNKNOWN_ERROR", "Erro desconhecido do servidor.", (int)response.StatusCode));

            var code = problem.Type?.Split(":").LastOrDefault() ?? "UNKNOWN_ERROR";
            return Result<ProfileResponseDTO>.Failure(new Error(code, problem.Type, (int)response.StatusCode));
        }
    }
}
