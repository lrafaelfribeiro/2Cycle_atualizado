namespace APP.Services.Routes
{
    public record PendingRouteSave(Guid PendingSaveId, Guid SuggestedRouteId, string? RouteName);

    public interface IRouteSaveCoordinator
    {
        IReadOnlyList<PendingRouteSave> GetPendingSaves();
        Guid StartSave(Guid suggestedRouteId, string? name);
    }
}