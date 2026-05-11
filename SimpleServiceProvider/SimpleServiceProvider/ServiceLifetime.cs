namespace SimpleServiceProvider
{
    /// <summary>
    /// Defines how the provider caches and returns instances of a registered service.
    /// </summary>
    public enum ServiceLifetime
    {
        /// <summary>
        /// A single instance is created on first resolve and reused for the lifetime of the root provider.
        /// </summary>
        Singleton,

        /// <summary>
        /// A single instance is created on first resolve within a scope and reused while the scope is alive.
        /// </summary>
        Scoped,

        /// <summary>
        /// A new instance is created on every resolve.
        /// </summary>
        Transient
    }
}
