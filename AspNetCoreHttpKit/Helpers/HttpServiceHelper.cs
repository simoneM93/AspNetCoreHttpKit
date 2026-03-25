using AspNetCoreHttpKit.Models;
using System.Net;

namespace AspNetCoreHttpKit.Helpers
{
    public static class HttpServiceHelper
    {
        /// <summary>
        /// Maps an <see cref="HttpResult{T}"/> to the appropriate typed exception.
        /// Call this when you prefer exceptions over the result pattern.
        /// </summary>
        public static void ThrowIfFailed<T>(HttpResult<T> result)
        {
            if (result.IsSuccess) return;

            throw result.StatusCode switch
            {
                HttpStatusCode.BadRequest => new HttpBadRequestException(result.ErrorMessage!),
                HttpStatusCode.Unauthorized => new HttpUnauthorizedException(result.ErrorMessage!),
                HttpStatusCode.Forbidden => new HttpForbiddenException(result.ErrorMessage!),
                HttpStatusCode.NotFound => new HttpNotFoundException(result.ErrorMessage!),
                HttpStatusCode.Conflict => new HttpConflictException(result.ErrorMessage!),
                HttpStatusCode.UnprocessableEntity => new HttpUnprocessableEntityException(result.ErrorMessage!),
                HttpStatusCode.TooManyRequests => new HttpTooManyRequestsException(result.ErrorMessage!),
                >= HttpStatusCode.InternalServerError => new HttpServerErrorException(result.StatusCode, result.ErrorMessage!),
                _ => new HttpServiceException(result.StatusCode, result.ErrorMessage!)
            };
        }

        /// <summary>
        /// Maps an <see cref="HttpResult"/> to the appropriate typed exception.
        /// </summary>
        public static void ThrowIfFailed(HttpResult result)
        {
            if (result.IsSuccess) return;

            throw result.StatusCode switch
            {
                HttpStatusCode.BadRequest => new HttpBadRequestException(result.ErrorMessage!),
                HttpStatusCode.Unauthorized => new HttpUnauthorizedException(result.ErrorMessage!),
                HttpStatusCode.Forbidden => new HttpForbiddenException(result.ErrorMessage!),
                HttpStatusCode.NotFound => new HttpNotFoundException(result.ErrorMessage!),
                HttpStatusCode.Conflict => new HttpConflictException(result.ErrorMessage!),
                HttpStatusCode.UnprocessableEntity => new HttpUnprocessableEntityException(result.ErrorMessage!),
                HttpStatusCode.TooManyRequests => new HttpTooManyRequestsException(result.ErrorMessage!),
                >= HttpStatusCode.InternalServerError => new HttpServerErrorException(result.StatusCode, result.ErrorMessage!),
                _ => new HttpServiceException(result.StatusCode, result.ErrorMessage!)
            };
        }
    }
}
