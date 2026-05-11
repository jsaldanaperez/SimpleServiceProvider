namespace SimpleServiceProvider
{
    internal class ServiceScope : IServiceScope
    {
        private bool _disposed;

        public ServiceScope(ServiceProvider provider)
        {
            Provider = provider;
        }

        public ServiceProvider Provider { get; }

        public void Dispose()
        {
            if (_disposed)
            {
                return;
            }
            _disposed = true;
            Provider.DisposeScopedInstances();
        }
    }
}
