using APP.Views;

namespace APP
{
    public partial class AppShell : Shell
    {
        public AppShell()
        {
            InitializeComponent();
            Routing.RegisterRoute("chronometer", typeof(ChronometerPage));
            Routing.RegisterRoute("activity-summary", typeof(ActivitySummaryPage));
            Routing.RegisterRoute("create-route", typeof(CreateRoutePage));
            Routing.RegisterRoute("routedetail", typeof(RouteDetailPage));
        }
    }
}
