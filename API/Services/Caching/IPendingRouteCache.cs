using API.Services.OpenRouteService;

namespace API.Services.Caching
{
    /// <summary>
    /// Guarda o resultado bruto da ORS entre o pedido de sugestão (preview) e o momento
    /// em que o utilizador decide guardar. SuggestedRouteId fica null enquanto a rota só
    /// existe em memória; é preenchido a seguir à primeira persistência, para que um
    /// pedido de "guardar" repetido com o mesmo pendingRouteId seja idempotente (não
    /// insere a rota duas vezes) em vez de falhar com "já expirou".
    /// </summary>
    public record PendingRoute(
        Guid CreatedByUserId,
        double OriginLatitude, double OriginLongitude,
        double DestinationLatitude, double DestinationLongitude,
        bool IsRoundTrip,
        double? RequestedDistanceMeters,
        ORSRouteResult OrsResult,
        Guid? SuggestedRouteId = null);

    public interface IPendingRouteCache
    {
        Guid Store(PendingRoute route);
        PendingRoute? TryGet(Guid pendingRouteId);
        void MarkPersisted(Guid pendingRouteId, Guid suggestedRouteId);
    }
}