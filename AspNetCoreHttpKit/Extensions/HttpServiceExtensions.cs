using AspNetCoreHttpKit.Interfaces;
using AspNetCoreHttpKit.Logging;
using AspNetCoreHttpKit.Models;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using StructLog.Interfaces;

namespace AspNetCoreHttpKit.Extensions
{
    public static class HttpServiceExtensions
    {
        /// <summary>
        /// Registers AspNetCoreHttpKit services with configuration from appsettings.json.
        ///
        /// Logger resolution (automatic):
        ///   - If IStructLog&lt;T&gt; is registered in DI → uses StructLog with typed EventCodes
        ///   - Otherwise → falls back to standard ILogger
        /// </summary>
        public static IServiceCollection AddAspNetCoreHttpKit(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(services);
            ArgumentNullException.ThrowIfNull(configuration);

            var section = configuration.GetSection("HttpServiceOptions");

            services.AddOptions<HttpServiceOptions>()
                .Bind(section)
                .ValidateDataAnnotations()
                .Validate(
                    options => options.TimeoutSeconds > 0,
                    "TimeoutSeconds must be greater than zero.")
                .Validate(
                    options => options.Clients.Values
                        .Where(c => c.TimeoutSeconds.HasValue)
                        .All(c => c.TimeoutSeconds!.Value > 0),
                    "All client TimeoutSeconds values must be greater than zero.")
                .ValidateOnStart();

            services.AddHttpClient();

            var httpServiceOptions = section.Get<HttpServiceOptions>() ?? new HttpServiceOptions();

            foreach (var (name, clientOptions) in httpServiceOptions.Clients)
            {
                services.AddHttpClient(name, client =>
                {
                    var baseUrl = clientOptions.BaseUrl ?? httpServiceOptions.BaseUrl;
                    if (!string.IsNullOrEmpty(baseUrl))
                        client.BaseAddress = new Uri(baseUrl);

                    client.Timeout = clientOptions.TimeoutSeconds.HasValue
                        ? TimeSpan.FromSeconds(clientOptions.TimeoutSeconds.Value)
                        : TimeSpan.FromSeconds(httpServiceOptions.TimeoutSeconds);
                });
            }

            // Named HttpClient default
            services.AddHttpClient("default", client =>
            {
                if (!string.IsNullOrEmpty(httpServiceOptions.BaseUrl))
                    client.BaseAddress = new Uri(httpServiceOptions.BaseUrl);

                client.Timeout = TimeSpan.FromSeconds(httpServiceOptions.TimeoutSeconds);
            });

            services.TryAddSingleton<StructLogHttpLogger>();
            services.TryAddSingleton<ILoggerHttpLogger>();

            services.TryAddSingleton<IHttpLogger>(sp =>
            {
                var structLog = sp.GetService<IStructLog<StructLogHttpLogger>>();
                if (structLog is not null)
                    return new StructLogHttpLogger(structLog);

                return sp.GetRequiredService<ILoggerHttpLogger>();
            });

            services.AddSingleton<IHttpService>(sp =>
            {
                var factory = sp.GetRequiredService<IHttpClientFactory>();
                var logger = sp.GetRequiredService<IHttpLogger>();
                var client = factory.CreateClient("default");
                return new HttpService(client, logger);
            });

            services.AddSingleton<IHttpServiceFactory, HttpServiceFactory>();

            return services;
        }

        /// <summary>
        /// Registers AspNetCoreHttpKit services with default options (no appsettings).
        /// </summary>
        public static IServiceCollection AddAspNetCoreHttpKit(this IServiceCollection services)
            => services.AddAspNetCoreHttpKit(new ConfigurationBuilder().Build());
    }
}
