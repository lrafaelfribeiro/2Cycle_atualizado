using APP.Services.Auth;
using APP.Services.Token;
using System.Net;
using System.Net.Http.Headers;

namespace APP.Core;

public class AuthHeaderHandler : DelegatingHandler
{
    private static readonly TimeSpan RefreshThreshold =
        TimeSpan.FromSeconds(30);

    private readonly ITokenStorageService _tokenStorage;
    private readonly IAuthService _authService;

    // Garante que apenas uma thread/request faz refresh de cada vez.
    private readonly SemaphoreSlim _refreshLock = new(1, 1);

    public AuthHeaderHandler(
        ITokenStorageService tokenStorage,
        IAuthService authService)
    {
        _tokenStorage = tokenStorage;
        _authService = authService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken ct)
    {
        /*
         * ============================================================
         * 1. OBTER TOKEN
         * ============================================================
         */

        var accessToken =
            await _tokenStorage.GetAccessTokenAsync();

        /*
         * ============================================================
         * 2. REFRESH PREVENTIVO
         *
         * Se o token já expirou ou vai expirar dentro dos próximos
         * 30 segundos, fazemos refresh antes de chamar a API.
         * ============================================================
         */

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            var expiresAt =
                await _tokenStorage.GetExpiresAtAsync();

            if (ShouldRefresh(expiresAt))
            {
                var refreshed =
                    await RefreshIfNeededAsync(
                        accessToken,
                        ct);

                if (refreshed)
                {
                    accessToken =
                        await _tokenStorage.GetAccessTokenAsync();
                }
                else
                {
                    /*
                     * Não conseguimos fazer refresh.
                     *
                     * Ainda podemos deixar o request seguir.
                     * Se o servidor devolver 401, temos o fallback
                     * abaixo.
                     */
                    accessToken =
                        await _tokenStorage.GetAccessTokenAsync();
                }
            }
        }

        /*
         * ============================================================
         * 3. ADICIONAR BEARER TOKEN
         * ============================================================
         */

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        /*
         * ============================================================
         * 4. REQUEST ORIGINAL
         * ============================================================
         */

        var response =
            await base.SendAsync(request, ct);

        /*
         * Se não for 401, acabou.
         */
        if (response.StatusCode != HttpStatusCode.Unauthorized)
            return response;

        /*
         * ============================================================
         * 5. FALLBACK: API DEVOLVEU 401
         * ============================================================
         *
         * Aqui tentamos refresh mesmo que o token ainda parecesse
         * válido localmente.
         */

        response.Dispose();

        /*
         * Guardamos o token que foi usado neste request.
         *
         * Isto é importante para concorrência:
         *
         * Request A → 401
         * Request B → 401
         *
         * A faz refresh.
         * B espera pelo lock.
         * Quando B entra, percebe que o token já mudou e NÃO faz
         * outro refresh.
         */

        var tokenUsedForRequest = accessToken;

        var refreshedAfter401 =
            await RefreshIfNeededAsync(
                tokenUsedForRequest,
                ct);

        if (!refreshedAfter401)
        {
            /*
             * Refresh falhou.
             *
             * Aqui devolvemos 401 para o serviço/viewmodel decidir
             * o que fazer (normalmente logout/navegação para login).
             */
            return CreateUnauthorizedResponse();
        }

        /*
         * ============================================================
         * 6. OBTER NOVO ACCESS TOKEN
         * ============================================================
         */

        var newAccessToken =
            await _tokenStorage.GetAccessTokenAsync();

        if (string.IsNullOrWhiteSpace(newAccessToken))
        {
            await _tokenStorage.ClearTokensAsync();

            return CreateUnauthorizedResponse();
        }

        /*
         * ============================================================
         * 7. RETRY DO REQUEST ORIGINAL
         * ============================================================
         */

        return await RetryRequestAsync(
            request,
            newAccessToken,
            ct);
    }

    /*
     * ================================================================
     * DECIDE SE É NECESSÁRIO REFRESH
     * ================================================================
     */

    private static bool ShouldRefresh(DateTime? expiresAt)
    {
        if (!expiresAt.HasValue)
            return false;

        var expiration =
            expiresAt.Value.Kind == DateTimeKind.Utc
                ? expiresAt.Value
                : expiresAt.Value.ToUniversalTime();

        return expiration <=
               DateTime.UtcNow.Add(RefreshThreshold);
    }

    /*
     * ================================================================
     * REFRESH COM CONTROLO DE CONCORRÊNCIA
     * ================================================================
     */

    private async Task<bool> RefreshIfNeededAsync(
        string? tokenBeforeRefresh,
        CancellationToken ct)
    {
        await _refreshLock.WaitAsync(ct);

        try
        {
            /*
             * ========================================================
             * OUTRO REQUEST PODE TER FEITO REFRESH
             * ========================================================
             *
             * Antes de fazer refresh, verificamos novamente o estado
             * do token.
             */

            var currentToken =
                await _tokenStorage.GetAccessTokenAsync();

            var currentExpiresAt =
                await _tokenStorage.GetExpiresAtAsync();

            /*
             * CASO 1:
             *
             * Outro request já mudou o token.
             *
             * Exemplo:
             *
             * Request A → refresh → token B
             * Request B → estava à espera do lock
             *
             * B vê que token B != token A.
             *
             * Não precisa fazer refresh novamente.
             */

            if (!string.IsNullOrWhiteSpace(tokenBeforeRefresh) &&
                !string.IsNullOrWhiteSpace(currentToken) &&
                currentToken != tokenBeforeRefresh)
            {
                /*
                 * Para o caso de refresh preventivo, confirmamos
                 * também que o novo token não está prestes a expirar.
                 */
                if (!ShouldRefresh(currentExpiresAt))
                    return true;
            }

            /*
             * ========================================================
             * CASO 2:
             *
             * O token ainda é válido e outro request acabou de
             * atualizá-lo.
             * ========================================================
             */

            if (!ShouldRefresh(currentExpiresAt))
            {
                /*
                 * Se estamos aqui devido a um 401, o token pode
                 * aparentemente estar válido, mas o servidor rejeitou-o.
                 *
                 * Nesse caso precisamos distinguir os dois casos.
                 */

                if (currentToken != tokenBeforeRefresh)
                    return true;

                /*
                 * Se tokenBeforeRefresh == currentToken e não está
                 * perto de expirar, então precisamos mesmo de fazer
                 * refresh porque chegámos aqui devido a um 401.
                 */
            }

            /*
             * ========================================================
             * OBTER REFRESH TOKEN
             * ========================================================
             */

            var refreshToken =
                await _tokenStorage.GetRefreshTokenAsync();

            if (string.IsNullOrWhiteSpace(refreshToken))
            {
                await _tokenStorage.ClearTokensAsync();
                return false;
            }

            /*
             * ========================================================
             * FAZER REFRESH
             * ========================================================
             */

            var result =
                await _authService.RefreshTokensAsync(
                    refreshToken,
                    ct);

            /*
             * O teu AuthService já guarda os novos tokens
             * dentro de PostAuthRequestAsync().
             */

            if (result.IsFailure)
            {
                await _tokenStorage.ClearTokensAsync();
                return false;
            }

            /*
             * Confirmar que realmente recebemos um novo token.
             */

            var newToken =
                await _tokenStorage.GetAccessTokenAsync();

            if (string.IsNullOrWhiteSpace(newToken))
            {
                await _tokenStorage.ClearTokensAsync();
                return false;
            }

            return true;
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            /*
             * Não fazemos throw aqui porque o handler deve transformar
             * uma falha de refresh numa sessão inválida.
             */
            await _tokenStorage.ClearTokensAsync();
            return false;
        }
        finally
        {
            _refreshLock.Release();
        }
    }

    /*
     * ================================================================
     * RETRY DO REQUEST ORIGINAL
     * ================================================================
     */

    private async Task<HttpResponseMessage> RetryRequestAsync(
        HttpRequestMessage originalRequest,
        string accessToken,
        CancellationToken ct)
    {
        var request =
            await CloneHttpRequestMessageAsync(
                originalRequest,
                ct);

        request.Headers.Authorization =
            new AuthenticationHeaderValue(
                "Bearer",
                accessToken);

        /*
         * IMPORTANTE:
         *
         * Usamos base.SendAsync() e NÃO SendAsync().
         *
         * Assim o retry não entra novamente neste handler e não
         * cria um ciclo infinito.
         */

        return await base.SendAsync(
            request,
            ct);
    }

    /*
     * ================================================================
     * CLONAR HTTP REQUEST
     * ================================================================
     *
     * HttpRequestMessage não deve ser simplesmente reutilizado
     * depois de ter sido enviado.
     */

    private static async Task<HttpRequestMessage>
        CloneHttpRequestMessageAsync(
            HttpRequestMessage original,
            CancellationToken ct)
    {
        var clone = new HttpRequestMessage(
            original.Method,
            original.RequestUri);

        /*
         * Headers
         */

        foreach (var header in original.Headers)
        {
            clone.Headers.TryAddWithoutValidation(
                header.Key,
                header.Value);
        }

        /*
         * Content
         */

        if (original.Content != null)
        {
            var content =
                await original.Content.ReadAsByteArrayAsync(ct);

            clone.Content =
                new ByteArrayContent(content);

            foreach (var header in original.Content.Headers)
            {
                clone.Content.Headers.TryAddWithoutValidation(
                    header.Key,
                    header.Value);
            }
        }

        /*
         * Algumas propriedades úteis.
         */

        clone.Version = original.Version;

        return clone;
    }

    /*
     * ================================================================
     * 401 RESPONSE
     * ================================================================
     */

    private static HttpResponseMessage
        CreateUnauthorizedResponse()
    {
        return new HttpResponseMessage(
            HttpStatusCode.Unauthorized);
    }
}
