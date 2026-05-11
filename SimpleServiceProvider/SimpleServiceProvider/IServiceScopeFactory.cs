namespace SimpleServiceProvider
{
    /// <summary>
    /// Creates <see cref="IServiceScope"/> instances. Resolvable from the root <see cref="ServiceProvider"/>.
    /// </summary>
    public interface IServiceScopeFactory
    {
        /// <summary>
        /// Creates a new <see cref="IServiceScope"/>.
        /// </summary>
        IServiceScope CreateScope();
    }
}
