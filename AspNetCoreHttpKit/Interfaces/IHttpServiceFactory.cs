namespace AspNetCoreHttpKit.Interfaces
{
    /// <summary>
    /// Resolves named <see cref="IHttpService"/> instances configured in appsettings.json.
    /// </summary>
    public interface IHttpServiceFactory
    {
        /// <summary>
        /// Returns the <see cref="IHttpService"/> instance configured for the given client name.
        /// </summary>
        /// <param name="name">The named client key as defined in HttpServiceOptions.Clients.</param>
        IHttpService Create(string name);
    }
}
