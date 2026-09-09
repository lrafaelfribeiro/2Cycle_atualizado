namespace APP.Components
{
    // Assume-se que RouteDifficulty existe no domínio partilhado (API/APP) com
    // esta ordem: VeryEasy, Easy, Moderate, Hard, VeryHard, Pro.
    // Ajusta o using consoante o namespace real do enum no projeto MAUI.
    using APP.Models;

    public partial class DifficultyStarsView : ContentView
    {
        public const int MaxStars = 5;

        private const string StarFilledImage = "star_filled.svg";
        private const string StarEmptyImage = "star_outline.svg";

        public static readonly BindableProperty DifficultyProperty =
            BindableProperty.Create(
                nameof(Difficulty),
                typeof(RouteDifficulty),
                typeof(DifficultyStarsView),
                RouteDifficulty.Easy,
                propertyChanged: OnDifficultyChanged);

        public static readonly BindableProperty IsProLevelProperty =
            BindableProperty.Create(
                nameof(IsProLevel),
                typeof(bool),
                typeof(DifficultyStarsView),
                false);

        public RouteDifficulty Difficulty
        {
            get => (RouteDifficulty)GetValue(DifficultyProperty);
            set => SetValue(DifficultyProperty, value);
        }

        public bool IsProLevel
        {
            get => (bool)GetValue(IsProLevelProperty);
            private set => SetValue(IsProLevelProperty, value);
        }

        public DifficultyStarsView()
        {
            InitializeComponent();
            RenderStars(Difficulty);
        }

        private static void OnDifficultyChanged(BindableObject bindable, object oldValue, object newValue)
        {
            if (bindable is DifficultyStarsView view && newValue is RouteDifficulty difficulty)
            {
                view.RenderStars(difficulty);
            }
        }

        private void RenderStars(RouteDifficulty difficulty)
        {
            IsProLevel = difficulty == RouteDifficulty.Pro;

            int filledCount = ResolveFilledStarCount(difficulty);

            StarsHost.Children.Clear();
            for (int i = 0; i < MaxStars; i++)
            {
                StarsHost.Children.Add(CreateStarImage(isFilled: i < filledCount));
            }
        }

        // Pura, testável isoladamente sem instanciar a View.
        public static int ResolveFilledStarCount(RouteDifficulty difficulty) => difficulty switch
        {
            RouteDifficulty.VeryEasy => 1,
            RouteDifficulty.Easy => 2,
            RouteDifficulty.Moderate => 3,
            RouteDifficulty.Hard => 4,
            RouteDifficulty.VeryHard => 5,
            RouteDifficulty.Pro => MaxStars, // Pro enche todas + badge à parte
            _ => throw new ArgumentOutOfRangeException(nameof(difficulty), difficulty, null)
        };

        private static Image CreateStarImage(bool isFilled) => new()
        {
            Source = isFilled ? StarFilledImage : StarEmptyImage,
            HeightRequest = 12,
            WidthRequest = 12
        };
    }
}