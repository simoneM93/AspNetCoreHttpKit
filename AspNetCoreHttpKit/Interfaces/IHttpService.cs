using AspNetCoreHttpKit.Models;

namespace AspNetCoreHttpKit.Interfaces
{
    /// <summary>
    /// Provides typed HTTP methods with automatic JSON serialization/deserialization.
    /// Use <see cref="IHttpServiceFactory"/> to resolve named clients configured in appsettings.json.
    /// </summary>
    public interface IHttpService
    {
        /// <summary>Sends a GET request and deserializes the response to <typeparamref name="T"/>.</summary>
        Task<HttpResult<T>> GetAsync<T>(string url, CancellationToken cancellationToken = default);

        /// <summary>Sends a POST request with a JSON body and deserializes the response to <typeparamref name="T"/>.</summary>
        Task<HttpResult<T>> PostAsync<T>(string url, object body, CancellationToken cancellationToken = default);

        /// <summary>Sends a PUT request with a JSON body and deserializes the response to <typeparamref name="T"/>.</summary>
        Task<HttpResult<T>> PutAsync<T>(string url, object body, CancellationToken cancellationToken = default);

        /// <summary>Sends a PATCH request with a JSON body and deserializes the response to <typeparamref name="T"/>.</summary>
        Task<HttpResult<T>> PatchAsync<T>(string url, object body, CancellationToken cancellationToken = default);

        /// <summary>Sends a DELETE request.</summary>
        Task<HttpResult> DeleteAsync(string url, CancellationToken cancellationToken = default);
    }
}
