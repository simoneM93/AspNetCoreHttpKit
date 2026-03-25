using System.Net;

namespace AspNetCoreHttpKit.Models
{
    /// <summary>
    /// Base exception for HTTP errors returned by the remote service.
    /// Throw this when you prefer exceptions over the result pattern.
    /// </summary>
    public class HttpServiceException(HttpStatusCode statusCode, string message, Exception? innerException = null) : Exception(message, innerException)
    {
        public HttpStatusCode StatusCode { get; } = statusCode;
    }

    /// <summary>400 Bad Request</summary>
    public sealed class HttpBadRequestException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.BadRequest, message, inner) { }

    /// <summary>401 Unauthorized</summary>
    public sealed class HttpUnauthorizedException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.Unauthorized, message, inner) { }

    /// <summary>403 Forbidden</summary>
    public sealed class HttpForbiddenException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.Forbidden, message, inner) { }

    /// <summary>404 Not Found</summary>
    public sealed class HttpNotFoundException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.NotFound, message, inner) { }

    /// <summary>409 Conflict</summary>
    public sealed class HttpConflictException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.Conflict, message, inner) { }

    /// <summary>422 Unprocessable Entity</summary>
    public sealed class HttpUnprocessableEntityException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.UnprocessableEntity, message, inner) { }

    /// <summary>429 Too Many Requests</summary>
    public sealed class HttpTooManyRequestsException(string message, Exception? inner = null) : HttpServiceException(HttpStatusCode.TooManyRequests, message, inner) { }

    /// <summary>500+ Server Error</summary>
    public sealed class HttpServerErrorException(HttpStatusCode statusCode, string message, Exception? inner = null) : HttpServiceException(statusCode, message, inner) { }
}
