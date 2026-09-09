using APP.DTOs;
using APP.DTOs.Routes;
using APP.DTOs.Routes.Requests;
using APP.DTOs.Routes.Responses;
using System.Net.Http.Json;

namespace APP.Services.Routes
{
    public class RouteService : IRouteService
    {
        private readonly HttpClient _httpClient;

        public RouteService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<RouteSuggestionResponse> SuggestAsync(RouteSuggestionRequest request, CancellationToken cancellationToken = default)
            => await PostAndReadAsync<RouteSuggestionResponse>("api/routes/suggest", request, cancellationToken);

        public async Task<RouteSuggestionResponse> SuggestRoundTripAsync(RouteRoundTripSuggestionRequest request, CancellationToken cancellationToken = default)
            => await PostAndReadAsync<RouteSuggestionResponse>("api/routes/round-trip", request, cancellationToken);

        public async Task<RouteDetailResponse> GetRouteDetailAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default)
            => await GetAndReadAsync<RouteDetailResponse>($"api/routes/{suggestedRouteId}", cancellationToken);

        public async Task<List<SavedRouteSummaryResponse>> GetSavedAsync(bool favoritesOnly, CancellationToken cancellationToken = default)
            => await GetAndReadAsync<List<SavedRouteSummaryResponse>>($"api/routes/saved?favoritesOnly={favoritesOnly}", cancellationToken);

        public async Task<SavedRouteDetailResponse> GetSavedRouteDetailAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default)
            => await GetAndReadAsync<SavedRouteDetailResponse>($"api/routes/saved/{suggestedRouteId}", cancellationToken);

        public async Task SaveAsync(Guid suggestedRouteId, string? name, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PostAsJsonAsync(
                $"api/routes/saved/{suggestedRouteId}", new SaveRouteRequest(name), cancellationToken);

            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task UnsaveAsync(Guid suggestedRouteId, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.DeleteAsync($"api/routes/saved/{suggestedRouteId}", cancellationToken);
            await EnsureSuccessAsync(response, cancellationToken);
        }

        public async Task SetFavoriteAsync(Guid suggestedRouteId, bool isFavorite, CancellationToken cancellationToken = default)
        {
            var url = $"api/routes/saved/{suggestedRouteId}/favorite";

            // POST marca como favorita, DELETE desmarca — espelha o padrão save/unsave. Sem corpo.
            var response = isFavorite
                ? await _httpClient.PostAsync(url, content: null, cancellationToken)
                : await _httpClient.DeleteAsync(url, cancellationToken);

            await EnsureSuccessAsync(response, cancellationToken);
        }

        // --- Helpers de ajuda: cada um resolve UMA coisa, para não repetir o padrão em cada método público ---

        private async Task<TResponse> PostAndReadAsync<TResponse>(string url, object body, CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsJsonAsync(url, body, cancellationToken);
            return await ReadRequiredAsync<TResponse>(response, cancellationToken);
        }

        private async Task<TResponse> GetAndReadAsync<TResponse>(string url, CancellationToken cancellationToken)
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            return await ReadRequiredAsync<TResponse>(response, cancellationToken);
        }

        private static async Task<TResponse> ReadRequiredAsync<TResponse>(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            await EnsureSuccessAsync(response, cancellationToken);

            var result = await response.Content.ReadFromJsonAsync<TResponse>(cancellationToken: cancellationToken);
            return result ?? throw new ApplicationException("Resposta vazia do servidor.");
        }

        private static async Task EnsureSuccessAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            if (response.IsSuccessStatusCode)
            {
                return;
            }

            var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
            throw new ApplicationException($"Erro {(int)response.StatusCode}: {errorBody}");
        }

        public async Task RenameAsync(Guid suggestedRouteId, string name, CancellationToken cancellationToken = default)
        {
            var response = await _httpClient.PatchAsJsonAsync(
                $"api/routes/saved/{suggestedRouteId}/name", new RenameRouteRequest(name), cancellationToken);

            await EnsureSuccessAsync(response, cancellationToken);
        }

        // Tenta extrair a mensagem amigável do ProblemDetails devolvido pelo backend
        // (GlobalExceptionHandler); se o corpo não vier nesse formato — ex: erro de
        // rede genérico antes de sequer chegar ao servidor — cai para uma mensagem simples.
        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken cancellationToken)
        {
            try
            {
                var problem = await response.Content.ReadFromJsonAsync<ProblemDetailsDTO>(cancellationToken: cancellationToken);
                if (!string.IsNullOrWhiteSpace(problem?.Detail))
                {
                    return problem.Detail;
                }
            }
            catch
            {
                // corpo não é JSON válido de ProblemDetails - cai para a mensagem genérica abaixo
            }

            return $"Erro {(int)response.StatusCode} ao comunicar com o servidor.";
        }
    }
}