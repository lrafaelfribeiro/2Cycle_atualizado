namespace APP.Services.Toast
{
    public class ToastService : IToastService
    {
        public async Task Show(string message)
        {
            await CommunityToolkit.Maui.Alerts.Toast.Make(message).Show();
        }
    }
}
