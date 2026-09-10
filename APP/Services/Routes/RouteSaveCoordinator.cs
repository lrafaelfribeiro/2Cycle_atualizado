using APP.Core.Messages;
using APP.Extensions;
using CommunityToolkit.Mvvm.Messaging;

namespace APP.Services.Routes
{
    public class RouteSaveCoordinator : IRouteSaveCoordinator
    {
        private readonly IRouteService _routeService;
        private readonly List<PendingRouteSave> _pendingSaves = new();
        private readonly Lock _lock = new();

        public RouteSaveCoordinator(IRouteService routeService)
        {
            _routeService = routeService;
        }

        public IReadOnlyList<PendingRouteSave> GetPendingSaves()
        {
            lock (_lock)
            {
                return _pendingSaves.ToList();
            }
        }

        public Guid StartSave(Guid suggestedRouteId, string? name)
        {
            var pending = new PendingRouteSave(Guid.NewGuid(), suggestedRouteId, name);

            lock (_lock)
            {
                _pendingSaves.Add(pending);
            }

            WeakReferenceMessenger.Default.Send(
                new RouteSaveStartedMessage(pending.PendingSaveId, name));

            RunSaveAsync(pending).FireAndForgetSafe();

            return pending.PendingSaveId;
        }

        private async Task RunSaveAsync(PendingRouteSave pending)
        {
            try
            {
                await _routeService.SaveAsync(pending.SuggestedRouteId, pending.RouteName);

                MainThread.BeginInvokeOnMainThread(() =>
                    WeakReferenceMessenger.Default.Send(
                        new RouteSaveCompletedMessage(pending.PendingSaveId, pending.SuggestedRouteId)));
            }
            catch (Exception ex)
            {
                MainThread.BeginInvokeOnMainThread(() =>
                    WeakReferenceMessenger.Default.Send(
                        new RouteSaveFailedMessage(pending.PendingSaveId, pending.SuggestedRouteId, ex.Message)));
            }
            finally
            {
                lock (_lock)
                {
                    _pendingSaves.RemoveAll(p => p.PendingSaveId == pending.PendingSaveId);
                }
            }
        }
    }
}