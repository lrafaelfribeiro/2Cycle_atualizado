namespace APP.Services.Navigation
{
    interface INavigationService
    {
        /// <summary>
        /// Navega para uma route registada no AppShell.
        /// </summary>
        Task GoToAsync(string route);

        /// <summary>
        /// Navega com parâmetros (ex: detalhes de um item).
        /// </summary>
        Task GoToAsync(string route, IDictionary<string, object> parameters);

        /// <summary>
        /// Volta atrás na stack de navegação.
        /// </summary>
        Task GoBackAsync();

        /// <summary>
        /// Volta para a raiz da navegação actual.
        /// </summary>
        Task GoBackToRootAsync();
    }
}
