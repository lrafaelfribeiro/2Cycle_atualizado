namespace APP.Services.Lifecycle
{
    public interface IAppLifecycleService
    {
        event EventHandler? HomeResetRequested;
        void NotifySleep();
        void NotifyResume();
    }
}