namespace LIB
{
    public record ApiResult<T>
    (
        bool IsSuccess,
        T? Data,
        string? ErrorMessage = null,
        List<string>? ValidationErrors = null
    );
}
