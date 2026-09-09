namespace APP.DTOs.Routes.Requests
{
    public sealed record RouteSuggestionRequest(
            double OriginLatitude,
            double OriginLongitude,
            double DestinationLatitude,
            double DestinationLongitude
        );
}