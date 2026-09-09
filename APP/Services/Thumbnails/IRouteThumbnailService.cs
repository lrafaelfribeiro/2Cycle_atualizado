namespace APP.Services.Thumbnails
{
    public interface IRouteThumbnailService
    {
        Task<string?> GetOrCreateThumbnailPathAsync(
            Guid routeId,
            IReadOnlyList<(double Lat, double Lon)> points,
            int widthPx = 180,
            int heightPx = 140,
            CancellationToken ct = default);
    }
}
