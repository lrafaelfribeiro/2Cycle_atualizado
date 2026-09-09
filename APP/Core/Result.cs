namespace APP.Core
{
    public record Result<T>
    {
        public bool IsSuccess
        {
            get; init;
        }
        public bool IsFailure => !IsSuccess;
        public T? Value
        {
            get; init;
        }
        public Error? Error
        {
            get; init;
        }

        private Result()
        {
        }

        public static Result<T> Success(T value) => new() { IsSuccess = true, Value = value };
        public static Result<T> Failure(Error error) => new() { IsSuccess = false, Error = error };
    }

    public record Error(string Code, string Message, int StatusCode);
}
