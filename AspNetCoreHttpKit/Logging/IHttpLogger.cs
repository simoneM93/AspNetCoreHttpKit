using System.Net;

namespace AspNetCoreHttpKit.Logging
{
    /// <summary>
    /// Internal logging abstraction that supports both ILogger and StructLog.
    /// Resolved automatically by DI — StructLog is used if registered, otherwise falls back to ILogger.
    /// </summary>
    public interface IHttpLogger
    {
        void LogRequest(HttpMethod method, string url, string? clientName);
        void LogSuccess(HttpMethod method, string url, HttpStatusCode statusCode);
        void LogWarning(HttpMethod method, string url, HttpStatusCode statusCode, string? error);
        void LogTimeout(HttpMethod method, string url);
        void LogException(HttpMethod method, string url, Exception ex);
    }
}
