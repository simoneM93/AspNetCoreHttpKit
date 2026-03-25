using AspNetCoreHttpKit.Interfaces;
using AspNetCoreHttpKit.Logging;
using AspNetCoreHttpKit.Models;
using Microsoft.Extensions.Options;

namespace AspNetCoreHttpKit
{
    /// <summary>
    /// Resolves named <see cref="IHttpService"/> instances using <see cref="IHttpClientFactory"/>.
    /// Each named client is configured with its own BaseUrl and Timeout from appsettings.json.
    /// Logging is handled via <see cref="IHttpLogger"/> — uses StructLog if registered,
    /// falls back to ILogger automatically.
    /// </summary>
    public sealed class HttpServiceFactory : IHttpServiceFactory
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly HttpServiceOptions _options;
        private readonly IHttpLogger _logger;

        public HttpServiceFactory(
            IHttpClientFactory httpClientFactory,
            IOptions<HttpServiceOptions> options,
            IHttpLogger logger)
        {
            ArgumentNullException.ThrowIfNull(httpClientFactory);
            ArgumentNullException.ThrowIfNull(options);
            ArgumentNullException.ThrowIfNull(logger);

            _httpClientFactory = httpClientFactory;
            _options = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Creates an <see cref="IHttpService"/> for the given named client.
        /// BaseUrl and Timeout are resolved from the named client config,
        /// falling back to the global options if not specified.
        /// </summary>
        public IHttpService Create(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);

            var httpClient = _httpClientFactory.CreateClient(name);

            var baseUrl = _options.ResolveBaseUrl(name);
            if (!string.IsNullOrEmpty(baseUrl))
                httpClient.BaseAddress = new Uri(baseUrl);

            httpClient.Timeout = _options.ResolveTimeout(name);

            return new HttpService(httpClient, _logger, clientName: name);
        }
    }
}
