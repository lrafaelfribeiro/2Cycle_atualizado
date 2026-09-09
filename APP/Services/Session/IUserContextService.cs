namespace APP.Services.Session
{
    public interface IUserContextService
    {
        Task<string?> GetCurrentUserIdAsync();
        Task<string?> GetCurrentUserNameAsync();
    }
}
