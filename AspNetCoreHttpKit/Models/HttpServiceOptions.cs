using System.ComponentModel.DataAnnotations;

namespace AspNetCoreHttpKit.Models
{
    /// <summary>
    /// Global configuration options for AspNetCoreHttpKit.
    /// Bind to the "HttpServiceOptions" section in appsettings.json.
    ///
    /// Example:
    /// "HttpServiceOptions": {
    ///   "BaseUrl": "https://api.myservice.com",
    ///   "TimeoutSeconds": 30,
    ///   "Clients": {
    ///     "PaymentApi": {
    ///       "BaseUrl": "https://api.payment.com",
    ///       "TimeoutSeconds": 10
    ///     },
    ///     "UserApi": {
    ///       "TimeoutSeconds": 5
    ///     }
    ///   }
    /// }
    /// </summary>
    public sealed class HttpServiceOptions
    {
        /// <summary>
        /// Global base URL used by the default client and by named clients
        /// that do not specify their own BaseUrl.
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Global timeout in seconds. Default: 30.
        /// </summary>
        [Range(1, int.MaxValue, ErrorMessage = "TimeoutSeconds must be greater than 0.")]
        public int TimeoutSeconds { get; set; } = 30;

        /// <summary>
        /// Named client configurations.
        /// Key = client name, Value = per-client overrides.
        /// </summary>
        public Dictionary<string, HttpClientOptions> Clients { get; set; } = [];

        /// <summary>
        /// Resolves the effective BaseUrl for a given client name.
        /// Priority: named client BaseUrl → global BaseUrl.
        /// </summary>
        internal string? ResolveBaseUrl(string? clientName)
        {
            if (!string.IsNullOrEmpty(clientName)
                && Clients.TryGetValue(clientName, out var client)
                && !string.IsNullOrEmpty(client.BaseUrl))
                return client.BaseUrl;

            return BaseUrl;
        }

        /// <summary>
        /// Resolves the effective timeout for a given client name.
        /// Priority: named client TimeoutSeconds → global TimeoutSeconds.
        /// </summary>
        internal TimeSpan ResolveTimeout(string? clientName)
        {
            if (!string.IsNullOrEmpty(clientName)
                && Clients.TryGetValue(clientName, out var client)
                && client.TimeoutSeconds.HasValue)
                return TimeSpan.FromSeconds(client.TimeoutSeconds.Value);

            return TimeSpan.FromSeconds(TimeoutSeconds);
        }
    }

    /// <summary>
    /// Configuration for a single named HTTP client.
    /// Inherits BaseUrl and TimeoutSeconds from the global options if not specified.
    /// </summary>
    public sealed class HttpClientOptions
    {
        /// <summary>
        /// Base URL for this named client.
        /// If null, falls back to the global BaseUrl.
        /// </summary>
        public string? BaseUrl { get; set; }

        /// <summary>
        /// Timeout in seconds for this named client.
        /// If null, falls back to the global TimeoutSeconds.
        /// </summary>
        public int? TimeoutSeconds { get; set; }
    }
}
