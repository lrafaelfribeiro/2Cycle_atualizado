namespace APP.Components;

public partial class CustomTabBar : ContentView
{
    public event EventHandler<string>? TabRequested;

    public CustomTabBar()
    {
        InitializeComponent();
    }

    private async void OnHomeTapped(object sender, EventArgs e)
    {
        RequestTab("home");
    }

    private async void OnActivitiesTapped(object sender, EventArgs e)
    {
        RequestTab("activities");
    }

    private async void OnRoutesTapped(object sender, EventArgs e)
    {
        RequestTab("routes");
    }

    private async void OnProfileTapped(object sender, EventArgs e)
    {
        RequestTab("profile");
    }

    private async void OnAddTapped(object sender, EventArgs e)
    {
        RequestTab("startActivity");
    }

    private void RequestTab(string key)
    {
        UpdateActiveTab(key);
        TabRequested?.Invoke(this, key);
    }

    public void SetActiveTab(string key) => UpdateActiveTab(key);

    private static readonly Color ActiveColor = Color.FromArgb("#A6E22E");
    private static readonly Color InactiveColor = Color.FromArgb("#A8ABB3");

    private void UpdateActiveTab(string route)
    {
        // Reset geral: tudo inativo
        SetTabColor(HomeIcon, HomeLabel, InactiveColor);
        SetTabColor(ActivitiesIcon, ActivitiesLabel, InactiveColor);
        SetTabColor(RoutesIcon, RoutesLabel, InactiveColor);
        SetTabColor(ProfileIcon, ProfileLabel, InactiveColor);

        // Ativa só a que corresponde à rota atual
        switch (route)
        {
            case "home":
                SetTabColor(HomeIcon, HomeLabel, ActiveColor);
                break;
            case "activities":
                SetTabColor(ActivitiesIcon, ActivitiesLabel, ActiveColor);
                break;
            case "routes":
                SetTabColor(RoutesIcon, RoutesLabel, ActiveColor);
                break;
            case "profile":
                SetTabColor(ProfileIcon, ProfileLabel, ActiveColor);
                break;
            case "startActivity":
                break;
        }
    }

    private void SetTabColor(CommunityToolkit.Maui.Behaviors.IconTintColorBehavior iconTint, Label label, Color color)
    {
        iconTint.TintColor = color;
        label.TextColor = color;
    }
}