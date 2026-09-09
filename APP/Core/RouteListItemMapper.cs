using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;
using APP.DTOs.Routes;
using APP.DTOs.Routes.Responses;
using APP.Models;

namespace APP.Mappers
{
    public static class RouteListItemMapper
    {
        private const string DistanceUnit = "km";
        private const string ElevationUnit = "m";
        private const double MetersPerKilometer = 1000.0;
        private const string UnnamedRouteTitle = "Rota sem nome";

        public static RouteListItem FromResponse(
            SavedRouteSummaryResponse response,
            ICommand openCommand,
            ICommand optionsCommand,
            ICommand toggleFavoriteCommand) => new()
        {
            Id = response.Id,
            SuggestedRouteId = response.SuggestedRouteId,
            Title = ResolveDisplayTitle(response.Name),
            DistanceLabel = FormatDistance(response.DistanceMeters),
            ElevationLabel = FormatElevation(response.ElevationGainMeters),
            SavedAtDisplay = FormatSavedAt(response.SavedAt),
            Difficulty = response.Difficulty,
            IsFavorite = response.IsFavorite,
            ThumbnailPath = response.ThumbnailPath,
            RoutePoints = MapPreviewPoints(response.PreviewPoints),
            OpenCommand = openCommand,
            OptionsCommand = optionsCommand,
            ToggleFavoriteCommand = toggleFavoriteCommand
        };

        private static string ResolveDisplayTitle(string? name) =>
            string.IsNullOrWhiteSpace(name) ? UnnamedRouteTitle : name;

        private static string FormatDistance(double meters) =>
            $"{meters / MetersPerKilometer:F1} {DistanceUnit}";

        private static string FormatElevation(double meters) =>
            $"{meters:F0} {ElevationUnit}";

        // Ajusta a formatação real ("há 3 dias" vs "12/03/2026") consoante
        // já tenhas um helper equivalente para ActivityListItem.DateDisplay.
        private static string FormatSavedAt(DateTime savedAt) =>
            savedAt.ToLocalTime().ToString("dd/MM/yyyy");

        private static List<Location> MapPreviewPoints(List<PreviewPointResponse> points) =>
            points.Select(p => new Location(p.Latitude, p.Longitude)).ToList();
    }
}