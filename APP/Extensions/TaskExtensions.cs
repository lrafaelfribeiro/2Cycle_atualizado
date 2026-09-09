namespace APP.Extensions
{
    public static class TaskExtensions
    {
        public static async void FireAndForgetSafe(this Task task)
        {
            try
            {
                await task;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Fire-and-forget falhou: {ex}");
            }
        }
    }
}