namespace APP.DTOs.Routes.Requests
{
    public sealed record RouteRoundTripSuggestionRequest(
            double OriginLatitude,
            double OriginLongitude,
            double DesiredDistanceMeters
        );
}