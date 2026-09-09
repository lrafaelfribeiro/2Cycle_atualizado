using APP.Interfaces;
using APP.Services.Lifecycle;
using APP.ViewModels;

namespace APP.Views;

public partial class MainTabPage : ContentPage
{ 
    private readonly IServiceProvider _services;
    private readonly Dictionary<string, ContentView> _cache = new();

    public MainTabPage(IAppLifecycleService lifecycleService)
    {
        InitializeComponent();
        _services = IPlatformApplication.Current!.Services;

        lifecycleService.HomeResetRequested += (_, _) => ShowTab("home");

        string lastTab = Preferences.Default.Get("last_active_tab", "home");
        ShowTab(lastTab);
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        TabBar.SetActiveTab(Preferences.Default.Get("last_active_tab", "home"));
    }
    private void OnTabRequested(object? sender, string tabKey)
    {
        ShowTab(tabKey);
    }

    private void ShowTab(string tabKey)
    {
        Preferences.Default.Set("last_active_tab", tabKey);

        if (!_cache.TryGetValue(tabKey, out var view))
        {
            view = tabKey switch
            {
                "home" => CreateAndWire<HomeView>(),
                "activities" => CreateAndWire<ActivitiesView>(),
                "routes" => CreateAndWire<RoutesView>(),
                "profile" => CreateAndWire<ProfileView>(),
                "startActivity" => CreateAndWire<StartActivityView>(),
                _ => throw new ArgumentException($"Tab desconhecida: {tabKey}")
            };
            _cache[tabKey] = view;
        }
        
        if (view is IRefreshableView refreshable)
        {
            _ = refreshable.RefreshAsync();
        }

        TabContentHost.Content = view;
    }

    private T CreateAndWire<T>() where T : ContentView
    {
        var view = _services.GetRequiredService<T>();
        if (view is INavigableView nav)
            nav.TabRequested += (s, key) => ShowTab(key);

        return view;
    }
}