using StructLog.Interfaces;
using System.Net;

namespace AspNetCoreHttpKit.Logging
{
    /// <summary>
    /// IHttpLogger implementation backed by StructLog.
    /// Used automatically when IStructLog&lt;T&gt; is registered in DI.
    /// </summary>
    internal sealed class StructLogHttpLogger : IHttpLogger
    {
        private readonly IStructLog<StructLogHttpLogger> _logger;

        public StructLogHttpLogger(IStructLog<StructLogHttpLogger> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        public void LogRequest(HttpMethod method, string url, string? clientName)
            => _logger.Info(
                $"{method.Method} {url}",
                HttpEventCodes.RequestCodeFor(method),
                new { Url = url, Client = clientName ?? "default" });

        public void LogSuccess(HttpMethod method, string url, HttpStatusCode statusCode)
            => _logger.Info(
                $"{method.Method} {url} — {(int)statusCode}",
                HttpEventCodes.RequestCodeFor(method),
                new { Url = url, StatusCode = (int)statusCode });

        public void LogWarning(HttpMethod method, string url, HttpStatusCode statusCode, string? error)
            => _logger.Warning(
                $"{method.Method} {url} — {(int)statusCode}",
                HttpEventCodes.ErrorCodeFor(method),
                new { Url = url, StatusCode = (int)statusCode, Error = error });

        public void LogTimeout(HttpMethod method, string url)
            => _logger.Error(
                $"{method.Method} {url} — timeout",
                HttpEventCodes.Timeout,
                data: new { Url = url, Method = method.Method });

        public void LogException(HttpMethod method, string url, Exception ex)
            => _logger.Error(
                $"{method.Method} {url} — unhandled exception",
                HttpEventCodes.Exception,
                ex,
                new { Url = url, Method = method.Method, ExceptionMessage = ex.Message });
    }
}
