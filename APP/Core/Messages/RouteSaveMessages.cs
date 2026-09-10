namespace APP.Core.Messages
{
    public record RouteSaveStartedMessage(Guid PendingSaveId, string? RouteName);

    public record RouteSaveCompletedMessage(Guid PendingSaveId, Guid SuggestedRouteId);

    public record RouteSaveFailedMessage(Guid PendingSaveId, Guid SuggestedRouteId, string ErrorMessage);
}