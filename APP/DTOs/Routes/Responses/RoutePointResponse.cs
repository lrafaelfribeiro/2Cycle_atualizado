namespace APP.DTOs.Routes.Responses
{
    public record RoutePointResponse(
            double Latitude,
            double Longitude,
            double? AltitudeMeters,
            int Sequence
        );
}