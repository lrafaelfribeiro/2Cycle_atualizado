namespace APP.Services.Lifecycle
{
    public class AppLifecycleService : IAppLifecycleService
    {
        private static readonly TimeSpan BackgroundResetThreshold = TimeSpan.FromMinutes(5);

        private DateTime? _sleptAtUtc;

        public event EventHandler? HomeResetRequested;

        public void NotifySleep()
        {
            _sleptAtUtc = DateTime.UtcNow;
        }

        public void NotifyResume()
        {
            if (_sleptAtUtc is { } sleptAt && DateTime.UtcNow - sleptAt > BackgroundResetThreshold)
            {
                HomeResetRequested?.Invoke(this, EventArgs.Empty);
            }

            _sleptAtUtc = null;
        }
    }
}