namespace OneRoof.Infrastructure.Persistence
{
    public readonly struct SaveResult
    {
        public bool IsSuccess { get; }

        public string ErrorMessage { get; }

        public static SaveResult Success() => new SaveResult(true, null);

        public static SaveResult Failure(string errorMessage) => new SaveResult(false, errorMessage);

        private SaveResult(bool success, string errorMessage)
        {
            IsSuccess = success;
            ErrorMessage = errorMessage;
        }
    }

    public readonly struct LoadResult<T>
    {
        public bool IsSuccess => ErrorReason == LoadErrorReason.None;

        public LoadErrorReason ErrorReason { get; }

        public string ErrorMessage { get; }

        public T Value { get; }

        public static LoadResult<T> Success(T value) => new LoadResult<T>(LoadErrorReason.None, null, value);

        public static LoadResult<T> Failure(LoadErrorReason reason, string errorMessage) => new LoadResult<T>(reason, errorMessage, default);

        private LoadResult(LoadErrorReason reason, string errorMessage, T value)
        {
            ErrorReason = reason;
            ErrorMessage = errorMessage;
            Value = value;
        }
    }
}
