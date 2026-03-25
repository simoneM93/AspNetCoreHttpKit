using Microsoft.Extensions.Logging;
using System.Net;

namespace AspNetCoreHttpKit.Logging
{
    /// <summary>
    /// IHttpLogger fallback implementation backed by the standard ILogger.
    /// Used automatically when StructLog is NOT registered in DI.
    /// </summary>
    internal sealed class ILoggerHttpLogger : IHttpLogger
    {
        private readonly ILogger<ILoggerHttpLogger> _logger;

        public ILoggerHttpLogger(ILogger<ILoggerHttpLogger> logger)
        {
            ArgumentNullException.ThrowIfNull(logger);
            _logger = logger;
        }

        public void LogRequest(HttpMethod method, string url, string? clientName)
            => _logger.LogInformation(
                "HTTP {Method} {Url} — client: {Client}",
                method.Method, url, clientName ?? "default");

        public void LogSuccess(HttpMethod method, string url, HttpStatusCode statusCode)
            => _logger.LogInformation(
                "HTTP {Method} {Url} — {StatusCode}",
                method.Method, url, (int)statusCode);

        public void LogWarning(HttpMethod method, string url, HttpStatusCode statusCode, string? error)
            => _logger.LogWarning(
                "HTTP {Method} {Url} — {StatusCode} {Error}",
                method.Method, url, (int)statusCode, error);

        public void LogTimeout(HttpMethod method, string url)
            => _logger.LogError(
                "HTTP {Method} {Url} — timeout",
                method.Method, url);

        public void LogException(HttpMethod method, string url, Exception ex)
            => _logger.LogError(ex,
                "HTTP {Method} {Url} — unhandled exception",
                method.Method, url);
    }
}
