namespace APP.Services.LocationTracking
{
    public interface IForegroundLocationTrackingService
    {
        bool IsTracking { get; }
        Task<bool> StartAsync(Guid activityId, string ownerUserId, bool autoPauseEnabled);
        Task PauseAsync();
        Task ResumeAsync();
        Task SetAutoPauseAsync(bool enabled);
        Task<Guid> StopAsync();
    }
}
