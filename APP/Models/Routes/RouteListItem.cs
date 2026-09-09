using System;
using System.Collections.Generic;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using APP.Models;

namespace APP.Models
{
    public partial class RouteListItem : ObservableObject
    {
        public Guid Id { get; set; }
        public Guid SuggestedRouteId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DistanceLabel { get; set; } = string.Empty;
        public string ElevationLabel { get; set; } = string.Empty;
        public string SavedAtDisplay { get; set; } = string.Empty;
        public RouteDifficulty Difficulty { get; set; }
        public List<Location> RoutePoints { get; set; } = new();
        public string? ThumbnailPath { get; set; }

        // Muda depois de o item já estar na lista → precisa de notificar a UI
        [ObservableProperty]
        private bool isFavorite;

        public ICommand? OpenCommand { get; set; }
        public ICommand? OptionsCommand { get; set; }
        public ICommand? ToggleFavoriteCommand { get; set; }
    }
}