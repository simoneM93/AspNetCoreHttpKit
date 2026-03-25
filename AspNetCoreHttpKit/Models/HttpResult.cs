using System.Net;

namespace AspNetCoreHttpKit.Models
{
    /// <summary>
    /// Represents the result of an HTTP call without a response body (e.g. DELETE).
    /// </summary>
    public sealed class HttpResult
    {
        public bool IsSuccess { get; }
        public HttpStatusCode StatusCode { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private HttpResult(bool isSuccess, HttpStatusCode statusCode, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static HttpResult Success(HttpStatusCode statusCode)
            => new(true, statusCode, null, null);

        public static HttpResult Failure(HttpStatusCode statusCode, string errorMessage, Exception? exception = null)
            => new(false, statusCode, errorMessage, exception);

        public HttpResult OnSuccess(Action action)
        {
            if (IsSuccess) action();
            return this;
        }

        public HttpResult OnError(Action<string, HttpStatusCode> action)
        {
            if (!IsSuccess) action(ErrorMessage ?? string.Empty, StatusCode);
            return this;
        }
    }

    /// <summary>
    /// Represents the result of an HTTP call with a typed response body.
    /// </summary>
    public sealed class HttpResult<T>
    {
        public bool IsSuccess { get; }
        public T? Data { get; }
        public HttpStatusCode StatusCode { get; }
        public string? ErrorMessage { get; }
        public Exception? Exception { get; }

        private HttpResult(bool isSuccess, T? data, HttpStatusCode statusCode, string? errorMessage, Exception? exception)
        {
            IsSuccess = isSuccess;
            Data = data;
            StatusCode = statusCode;
            ErrorMessage = errorMessage;
            Exception = exception;
        }

        public static HttpResult<T> Success(T data, HttpStatusCode statusCode)
            => new(true, data, statusCode, null, null);

        public static HttpResult<T> Failure(HttpStatusCode statusCode, string errorMessage, Exception? exception = null)
            => new(false, default, statusCode, errorMessage, exception);

        public HttpResult<T> OnSuccess(Action<T?> action)
        {
            if (IsSuccess) action(Data);
            return this;
        }

        public HttpResult<T> OnError(Action<string, HttpStatusCode> action)
        {
            if (!IsSuccess) action(ErrorMessage ?? string.Empty, StatusCode);
            return this;
        }
    }
}
