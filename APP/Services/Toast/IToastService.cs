namespace APP.Services.Toast
{
    public interface IToastService
    {
        /// <summary>
        ///     Aparece uma notificacao toast no dispositivo com a mensagem indicada.
        /// </summary>
        /// <param name="message">Mensagem que aparece no Toast</param>
        /// <returns></returns>
        Task Show(string message);
    }
}
