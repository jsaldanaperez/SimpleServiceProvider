using System;

namespace SimpleServiceProvider
{
    /// <summary>
    /// Represents a disposable scope inside which scoped services share a single instance.
    /// Disposing the scope releases its scoped instances.
    /// </summary>
    public interface IServiceScope : IDisposable
    {
        /// <summary>
        /// The scoped <see cref="ServiceProvider"/>. Use this to resolve services within the scope.
        /// </summary>
        ServiceProvider Provider { get; }
    }
}
