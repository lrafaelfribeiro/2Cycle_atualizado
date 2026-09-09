using Microsoft.Extensions.Caching.Memory;

namespace API.Services.Caching
{
    public class PendingRouteCache : IPendingRouteCache
    {
        private static readonly TimeSpan CacheTtl = TimeSpan.FromMinutes(15);
        private const string CacheKeyPrefix = "pending-route:";

        private readonly IMemoryCache _memoryCache;

        public PendingRouteCache(IMemoryCache memoryCache)
        {
            _memoryCache = memoryCache;
        }

        public Guid Store(PendingRoute route)
        {
            var pendingRouteId = Guid.NewGuid();
            _memoryCache.Set(BuildKey(pendingRouteId), route, CacheTtl);
            return pendingRouteId;
        }

        public PendingRoute? TryGet(Guid pendingRouteId)
        {
            return _memoryCache.TryGetValue(BuildKey(pendingRouteId), out PendingRoute? route)
                ? route
                : null;
        }

        public void MarkPersisted(Guid pendingRouteId, Guid suggestedRouteId)
        {
            var existing = TryGet(pendingRouteId);
            if (existing == null)
            {
                return; // já expirou entretanto - nada a marcar
            }

            _memoryCache.Set(BuildKey(pendingRouteId), existing with { SuggestedRouteId = suggestedRouteId }, CacheTtl);
        }

        private static string BuildKey(Guid pendingRouteId) => $"{CacheKeyPrefix}{pendingRouteId}";
    }
}