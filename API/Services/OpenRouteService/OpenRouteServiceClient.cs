using System.Text.Json;
using System.Text.Json.Nodes;

namespace API.Services.OpenRouteService
{
    public class OpenRouteServiceClient : IOpenRouteServiceClient
    {
        private const int DefaultRoundTripPoints = 5;
        private const int MinCoordinatesForRoute = 2;

        private readonly HttpClient _httpClient;

        public OpenRouteServiceClient(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<ORSRouteResult> GetCyclingRouteAsync(
            IReadOnlyList<ORSCoordinate> coordinates,
            CancellationToken cancellationToken = default)
        {
            if (coordinates.Count < MinCoordinatesForRoute)
            {
                throw new ArgumentException(
                    $"São necessárias pelo menos {MinCoordinatesForRoute} coordenadas para calcular uma rota.",
                    nameof(coordinates));
            }

            var body = new
            {
                coordinates = ToOrsCoordinateArray(coordinates),
                elevation = true
            };

            return await SendAndParseAsync(body, cancellationToken);
        }

        public async Task<ORSRouteResult> GetRoundTripRouteAsync(
            double originLat, double originLon,
            double desiredLengthMeters,
            CancellationToken cancellationToken = default)
        {
            var body = new
            {
                coordinates = new[]
                {
                    new[] { originLon, originLat } // round trip só quer 1 coordenada
                },
                elevation = true,
                options = new
                {
                    round_trip = new
                    {
                        length = desiredLengthMeters,
                        points = DefaultRoundTripPoints,
                        seed = Random.Shared.Next() // variação a cada pedido
                    }
                }
            };

            return await SendAndParseAsync(body, cancellationToken);
        }

        // A ORS espera [longitude, latitude] por ponto — inverte a ordem "natural" lat/lon.
        private static double[][] ToOrsCoordinateArray(IReadOnlyList<ORSCoordinate> coordinates)
        {
            return coordinates
                .Select(c => new[] { c.Longitude, c.Latitude })
                .ToArray();
        }

        private async Task<ORSRouteResult> SendAndParseAsync(object requestBody, CancellationToken cancellationToken)
        {
            var response = await _httpClient.PostAsJsonAsync("v2/directions/cycling-regular/geojson", requestBody, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorBody = await response.Content.ReadAsStringAsync(cancellationToken);
                throw new InvalidOperationException($"OpenRouteService devolveu erro: {(int)response.StatusCode} - {errorBody}");
            }

            var json = await response.Content.ReadAsStringAsync(cancellationToken);
            var root = JsonNode.Parse(json)!;

            var feature = root["features"]![0]!;
            var coordinatesArray = feature["geometry"]!["coordinates"]!.AsArray();
            var distanceMeters = feature["properties"]!["summary"]!["distance"]!.GetValue<double>();

            var points = ParseRoutePoints(coordinatesArray);

            return new ORSRouteResult(distanceMeters, points);
        }

        // Extraído do SendAndParseAsync original — o parsing por ponto já era um bloco
        // autocontido dentro do foreach, só lhe dei nome e função própria.
        private static List<ORSRoutePoint> ParseRoutePoints(JsonArray coordinatesArray)
        {
            var points = new List<ORSRoutePoint>(coordinatesArray.Count);

            foreach (var coord in coordinatesArray)
            {
                var arr = coord!.AsArray();
                double lon = arr[0]!.GetValue<double>();
                double lat = arr[1]!.GetValue<double>();
                double? altitude = arr.Count > 2 ? arr[2]!.GetValue<double>() : null;

                points.Add(new ORSRoutePoint(lat, lon, altitude));
            }

            return points;
        }
    }
}