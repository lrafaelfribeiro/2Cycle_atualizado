using System.Windows.Input;

namespace APP.Models
{
    public class ActivityListItem
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string DateDisplay { get; set; } = string.Empty;
        public string DistanceDisplay { get; set; } = string.Empty;
        public string DurationDisplay { get; set; } = string.Empty;
        public string ElevationDisplay { get; set; } = string.Empty;
        public List<Location> RoutePoints { get; set; } = new();

        // Caminho do thumbnail gerado pelo RouteThumbnailService (SkiaSharp), cacheado em disco.
        // Null quando a atividade não tem pontos suficientes para gerar rota.
        public string? ThumbnailPath { get; set; }


        public ICommand? OpenCommand { get; set; }
        public ICommand? OptionsCommand { get; set; }
    }
}
