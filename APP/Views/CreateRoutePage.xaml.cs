using APP.ViewModels;
using System.Globalization;
using System.Text.Json;
using System.Web;

namespace APP.Views
{
    public partial class CreateRoutePage : ContentPage
    {
        private readonly CreateRouteViewModel _viewModel;
        private double _collapsedOffset;
        private double _panStartY;
        private bool _isAnimating;

        private readonly Queue<(long TimestampMs, double Y)> _panSamples = new();

        private const long VelocityWindowMs = 80;
        private const double FlingVelocityThreshold = 0.3;

        private const string SheetAnimationHandle = "SheetTranslate";

        public CreateRoutePage(CreateRouteViewModel viewModel)
        {
            InitializeComponent();
            _viewModel = viewModel;
            BindingContext = _viewModel;
        }

        private void OnSheetSizeChanged(object? sender, EventArgs e)
        {
            _collapsedOffset = Math.Max(0, SheetPanel.Height - 50);

            // Se não há animação em curso, sincroniza a posição imediatamente
            // com o novo offset. Isto cobre o caso de o conteúdo do sheet mudar
            // de altura (ex: trocar "ponto a ponto" -> "ida e volta") enquanto
            // o painel está colapsado — sem isto, o TranslationY fica "preso"
            // ao valor calculado antes da mudança de conteúdo.
            if (!_isAnimating && !_viewModel.IsSheetExpanded)
            {
                SheetPanel.TranslationY = _collapsedOffset;
            }
        }

        protected override async void OnAppearing()
        {
            base.OnAppearing();

            using var stream = await FileSystem.OpenAppPackageFileAsync("Maps/create-route-map.html");
            using var reader = new StreamReader(stream);
            string html = await reader.ReadToEndAsync();

            MapView.Source = new HtmlWebViewSource
            {
                Html = html,
                BaseUrl = "https://localhost/"
            };

            _viewModel.PropertyChanged += async (s, e) =>
            {
                if (e.PropertyName == nameof(CreateRouteViewModel.IsSheetExpanded))
                {
                    double targetY = _viewModel.IsSheetExpanded ? 0 : _collapsedOffset;
                    await AnimateSheetToAsync(targetY);
                }
            };

            _viewModel.DrawRouteRequested += async points =>
            {
                var json = JsonSerializer.Serialize(
                    points,
                    new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                    });

                await MapView.EvaluateJavaScriptAsync($"drawRoute({json})");
            };

            _viewModel.ModeChanged += async isRoundTrip =>
                await MapView.EvaluateJavaScriptAsync($"setMode('{(isRoundTrip ? "round-trip" : "point-to-point")}')");

            _viewModel.PointsSwapped += async (originLat, originLng, destLat, destLng) =>
                await MapView.EvaluateJavaScriptAsync(
                    $"setPoints({Inv(originLat)}, {Inv(originLng)}, {Inv(destLat)}, {Inv(destLng)})");

            _viewModel.LocationRequested += async (lat, lng) =>
                await MapView.EvaluateJavaScriptAsync($"centerOnLocation({Inv(lat)}, {Inv(lng)})");

            _viewModel.LockChanged += async isLocked =>
                await MapView.EvaluateJavaScriptAsync($"setLocked({(isLocked ? "true" : "false")})");

            _viewModel.MapClearRequested += async () =>
                await MapView.EvaluateJavaScriptAsync("clearMap()");
        }

        private async Task AnimateSheetToAsync(double targetY)
        {
            // Aborta qualquer animação anterior do mesmo handle antes de começar
            // uma nova — evita duas TranslateToAsync a "lutar" pelo mesmo valor
            // se o utilizador trocar de estado rapidamente (double tap, etc.)
            this.AbortAnimation(SheetAnimationHandle);

            _isAnimating = true;
            try
            {
                await SheetPanel.TranslateToAsync(0, targetY, 200, Easing.CubicOut);
            }
            finally
            {
                _isAnimating = false;

                // Ao terminar a animação, o tamanho do conteúdo pode ter mudado
                // entretanto (ex: trocar de modo a meio da animação) — reaplica
                // o offset correto para garantir consistência final.
                if (!_viewModel.IsSheetExpanded)
                {
                    SheetPanel.TranslationY = _collapsedOffset;
                }
            }
        }

        private void OnWebViewNavigating(object? sender, WebNavigatingEventArgs e)
        {
            if (!e.Url.StartsWith("app://message", StringComparison.OrdinalIgnoreCase))
                return;

            e.Cancel = true;

            var uri = new Uri(e.Url);
            var query = HttpUtility.ParseQueryString(uri.Query);
            string? rawMessage = query["data"];

            if (!string.IsNullOrEmpty(rawMessage))
            {
                string decoded = HttpUtility.UrlDecode(rawMessage);
                _viewModel.OnMapTapped(decoded);
            }
        }

        private static string Inv(double value) => value.ToString(CultureInfo.InvariantCulture);

        private void OnHandlePanUpdated(object? sender, PanUpdatedEventArgs e)
        {
            switch (e.StatusType)
            {
                case GestureStatus.Started:
                    this.AbortAnimation(SheetAnimationHandle);
                    _isAnimating = false;
                    _panStartY = SheetPanel.TranslationY;

                    _panSamples.Clear();
                    _panSamples.Enqueue((Environment.TickCount64, _panStartY));
                    break;

                case GestureStatus.Running:
                    double newY = Math.Clamp(_panStartY + e.TotalY, 0, _collapsedOffset);
                    SheetPanel.TranslationY = newY;

                    long now = Environment.TickCount64;
                    _panSamples.Enqueue((now, newY));

                    // descarta amostras fora da janela de tempo — só nos interessa
                    // o troço mais recente do gesto para calcular a velocidade "efetiva"
                    while (_panSamples.Count > 1 && now - _panSamples.Peek().TimestampMs > VelocityWindowMs)
                    {
                        _panSamples.Dequeue();
                    }
                    break;

                case GestureStatus.Completed:
                case GestureStatus.Canceled:
                    bool shouldExpand = DecideSnapDirection();

                    if (_viewModel.IsSheetExpanded == shouldExpand)
                    {
                        // Estado no ViewModel já é o pretendido -> PropertyChanged não dispara
                        // (o valor não muda), por isso forçamos o snap manualmente, senão o
                        // painel fica preso na posição onde o dedo o largou.
                        _ = AnimateSheetToAsync(shouldExpand ? 0 : _collapsedOffset);
                    }
                    else
                    {
                        _viewModel.IsSheetExpanded = shouldExpand; // dispara PropertyChanged -> animação
                    }

                    _panSamples.Clear();
                    break;
            }
        }

        private bool DecideSnapDirection()
        {
            double velocity = CalculateWindowVelocity();

            // Um flick claro vence a posição, mesmo que o dedo mal se tenha movido.
            // Velocidade negativa = a mover para cima = intenção de abrir.
            if (velocity < -FlingVelocityThreshold)
                return true;

            if (velocity > FlingVelocityThreshold)
                return false;

            // Sem flick significativo: decide pela posição, com threshold assimétrico
            // (mais fácil abrir do que fechar — mais natural ao toque)
            return SheetPanel.TranslationY < _collapsedOffset * 0.35;
        }

        private double CalculateWindowVelocity()
        {
            if (_panSamples.Count < 2)
                return 0;

            var oldest = _panSamples.Peek();
            var newest = _panSamples.Last();

            long deltaTime = newest.TimestampMs - oldest.TimestampMs;
            if (deltaTime <= 0)
                return 0;

            double deltaY = newest.Y - oldest.Y;
            return deltaY / deltaTime; // px/ms
        }
    }
}