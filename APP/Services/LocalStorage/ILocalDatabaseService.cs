using SQLite;

namespace APP.Services.LocalStorage
{
    public interface ILocalDatabaseService
    {
        Task<SQLiteAsyncConnection> GetConnectionAsync();
    }
}
