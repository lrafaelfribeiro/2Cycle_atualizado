namespace API.DTOs.Routes.Requests
{
    public sealed record RouteSuggestionRequest(
           double OriginLatitude,
           double OriginLongitude,
           double DestinationLatitude,
           double DestinationLongitude,
            List<RouteWaypoint>? Waypoints = null
        );
}