using Microsoft.Maui.Devices.Sensors;

namespace APP.Extensions
{
    public static class LocationExtensions
    {
        // Ponte entre o tipo GPS nativo do MAUI (Location) e o tuplo simples
        // (Latitude, Longitude) que RouteMapHtmlBuilder e outros helpers usam,
        // desacoplando-os de Microsoft.Maui.Devices.Sensors.
        public static List<(double Latitude, double Longitude)> ToLatLongTuples(this List<Location> locations) =>
            locations.Select(l => (l.Latitude, l.Longitude)).ToList();
    }
}