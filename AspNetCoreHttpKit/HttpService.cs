using AspNetCoreHttpKit.Interfaces;
using AspNetCoreHttpKit.Logging;
using AspNetCoreHttpKit.Models;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace AspNetCoreHttpKit
{
    /// <summary>
    /// Default implementation of <see cref="IHttpService"/>.
    /// Wraps <see cref="HttpClient"/> with automatic JSON serialization,
    /// structured logging (StructLog if available, ILogger as fallback),
    /// timeout per call, and typed error handling.
    /// </summary>
    public sealed class HttpService : IHttpService
    {
        private readonly HttpClient _httpClient;
        private readonly IHttpLogger _logger;
        private readonly string? _clientName;

        private static readonly JsonSerializerOptions _jsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase
        };

        public HttpService(HttpClient httpClient, IHttpLogger logger, string? clientName = null)
        {
            ArgumentNullException.ThrowIfNull(httpClient);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClient = httpClient;
            _logger = logger;
            _clientName = clientName;
        }

        public async Task<HttpResult<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default)
            => await SendAsync<T>(HttpMethod.Get, url, body: null, cancellationToken);

        public async Task<HttpResult<T>> PostAsync<T>(string url, object body, CancellationToken cancellationToken = default)
            => await SendAsync<T>(HttpMethod.Post, url, body, cancellationToken);

        public async Task<HttpResult<T>> PutAsync<T>(string url, object body, CancellationToken cancellationToken = default)
            => await SendAsync<T>(HttpMethod.Put, url, body, cancellationToken);

        public async Task<HttpResult<T>> PatchAsync<T>(string url, object body, CancellationToken cancellationToken = default)
            => await SendAsync<T>(HttpMethod.Patch, url, body, cancellationToken);

        public async Task<HttpResult> DeleteAsync(string url, CancellationToken cancellationToken = default)
        {
            try
            {
                _logger.LogRequest(HttpMethod.Delete, url, _clientName);

                var response = await _httpClient.DeleteAsync(url, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    _logger.LogSuccess(HttpMethod.Delete, url, response.StatusCode);
                    return HttpResult.Success(response.StatusCode);
                }

                var error = await ReadErrorAsync(response);
                _logger.LogWarning(HttpMethod.Delete, url, response.StatusCode, error);
                return HttpResult.Failure(response.StatusCode, error);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogException(HttpMethod.Delete, url, ex);
                return HttpResult.Failure(HttpStatusCode.InternalServerError, ex.Message, ex);
            }
        }

        // ----------------------------------------------------------
        // Core send method
        // ----------------------------------------------------------

        private async Task<HttpResult<T>> SendAsync<T>(
            HttpMethod method,
            string url,
            object? body,
            CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogRequest(method, url, _clientName);

                var request = new HttpRequestMessage(method, url);

                if (body is not null)
                    request.Content = JsonContent.Create(body, options: _jsonOptions);

                var response = await _httpClient.SendAsync(request, cancellationToken);

                if (response.IsSuccessStatusCode)
                {
                    var data = await response.Content.ReadFromJsonAsync<T>(_jsonOptions, cancellationToken);
                    _logger.LogSuccess(method, url, response.StatusCode);
                    return HttpResult<T>.Success(data!, response.StatusCode);
                }

                var error = await ReadErrorAsync(response);
                _logger.LogWarning(method, url, response.StatusCode, error);
                return HttpResult<T>.Failure(response.StatusCode, error);
            }
            catch (TaskCanceledException ex) when (!cancellationToken.IsCancellationRequested)
            {
                _logger.LogTimeout(method, url);
                return HttpResult<T>.Failure(HttpStatusCode.RequestTimeout, "The request timed out.", ex);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogException(method, url, ex);
                return HttpResult<T>.Failure(HttpStatusCode.InternalServerError, ex.Message, ex);
            }
        }

        private static async Task<string> ReadErrorAsync(HttpResponseMessage response)
        {
            var body = await response.Content.ReadAsStringAsync();
            return string.IsNullOrWhiteSpace(body)
                ? $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}"
                : body;
        }

        private static HttpServiceException BuildException(HttpStatusCode statusCode, string message)
            => statusCode switch
            {
                HttpStatusCode.BadRequest => new HttpBadRequestException(message),
                HttpStatusCode.Unauthorized => new HttpUnauthorizedException(message),
                HttpStatusCode.Forbidden => new HttpForbiddenException(message),
                HttpStatusCode.NotFound => new HttpNotFoundException(message),
                HttpStatusCode.Conflict => new HttpConflictException(message),
                HttpStatusCode.UnprocessableEntity => new HttpUnprocessableEntityException(message),
                HttpStatusCode.TooManyRequests => new HttpTooManyRequestsException(message),
                >= HttpStatusCode.InternalServerError => new HttpServerErrorException(statusCode, message),
                _ => new HttpServiceException(statusCode, message)
            };
    }
}
