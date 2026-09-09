namespace APP.Services.Session
{
    public interface ISessionService
    {
        Task<bool> EnsureSessionIsValidAsync(CancellationToken cancellationToken);
    }
}