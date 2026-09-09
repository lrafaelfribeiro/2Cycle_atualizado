using APP.Models.Local;
using SQLite;

namespace APP.Services.LocalStorage
{
    public class LocalDatabaseService : ILocalDatabaseService
    {
        private SQLiteAsyncConnection? _connection;
        private readonly SemaphoreSlim _initLock = new(1, 1);

        public async Task<SQLiteAsyncConnection> GetConnectionAsync()
        {
            if (_connection is not null)
                return _connection;

            await _initLock.WaitAsync();
            try
            {
                if (_connection is not null)
                    return _connection;

                var dbPath = Path.Combine(FileSystem.AppDataDirectory, "local.db3");
                var connection = new SQLiteAsyncConnection(dbPath);

                await connection.CreateTableAsync<LocalActivity>();
                await connection.CreateTableAsync<LocalTrackSegment>();
                await connection.CreateTableAsync<LocalTrackPoint>();

                _connection = connection;
                return _connection;
            }
            finally
            {
                _initLock.Release();
            }
        }
    }
}
