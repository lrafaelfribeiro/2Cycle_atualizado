using System.Windows.Input;
using APP.Models;
using CommunityToolkit.Mvvm.ComponentModel;

namespace APP.Models.Routes
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

        // Novo: identifica um placeholder de save em progresso; null nos itens reais
        public Guid? PendingSaveId { get; private init; }

        [ObservableProperty]
        private RouteListItemState state = RouteListItemState.Ready;

        [ObservableProperty]
        private string? saveErrorMessage;

        [ObservableProperty]
        private bool isFavorite;

        public ICommand? OpenCommand { get; set; }
        public ICommand? OptionsCommand { get; set; }
        public ICommand? ToggleFavoriteCommand { get; set; }

        public static RouteListItem CreatePlaceholder(Guid pendingSaveId, string? name) => new()
        {
            PendingSaveId = pendingSaveId,
            Title = string.IsNullOrWhiteSpace(name) ? "A criar a sua rota..." : name,
            State = RouteListItemState.Pending
        };
        public enum RouteListItemState
        {
            Pending,
            Ready,
            Failed
        }
    }
}
