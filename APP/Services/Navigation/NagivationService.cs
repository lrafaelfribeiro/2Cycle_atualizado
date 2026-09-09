namespace APP.Services.Navigation
{
    class NagivationService : INavigationService
    {
        public Task GoToAsync(string route)
        {
            // Shell.Current pode ser null em background ou durante inicialização
            if (Shell.Current is null)
            {
                throw new InvalidOperationException(
                    "Shell.Current é null. Verifica se a AppShell está definida como MainPage.");
            }

            return Shell.Current.GoToAsync(route);
        }

        public Task GoToAsync(string route, IDictionary<string, object> parameters)
        {
            if (Shell.Current is null)
                throw new InvalidOperationException("Shell.Current é null.");

            return Shell.Current.GoToAsync(route, parameters);
        }

        public Task GoBackAsync()
        {
            if (Shell.Current is null)
                throw new InvalidOperationException("Shell.Current é null.");

            return Shell.Current.Navigation.PopAsync();
        }

        public Task GoBackToRootAsync()
        {
            if (Shell.Current is null)
                throw new InvalidOperationException("Shell.Current é null.");

            return Shell.Current.Navigation.PopToRootAsync();
        }
    }
}
