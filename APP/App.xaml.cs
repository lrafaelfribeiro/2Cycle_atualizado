using APP.Core.Messages;
using APP.Services.Lifecycle;
using CommunityToolkit.Mvvm.Messaging;


namespace APP
{
    public partial class App : Application
    {
        private readonly IAppLifecycleService _lifecycleService;

        public App(IAppLifecycleService lifecycleService)
        {
            InitializeComponent();
            _lifecycleService = lifecycleService;
        }

        protected override Window CreateWindow(IActivationState? activationState)
        {
            var window = new Window(new AppShell());
            window.Resumed += (_, _) => WeakReferenceMessenger.Default.Send(new AppResumedMessage());
            window.Activated += (_, _) => WeakReferenceMessenger.Default.Send(new AppResumedMessage());
            return window;
        }

        protected override void OnSleep() => _lifecycleService.NotifySleep();

        protected override void OnResume() => _lifecycleService.NotifyResume();
    }
}