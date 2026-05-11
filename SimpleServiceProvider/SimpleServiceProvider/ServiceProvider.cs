using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;

namespace SimpleServiceProvider
{
    /// <summary>
    /// Dependency injection provider.
    /// </summary>
    public class ServiceProvider : IServiceScopeFactory
    {
        private readonly ServiceProvider _root;

        private readonly IDictionary<Type, Type> _serviceDefinitions;
        private readonly IDictionary<Type, ServiceLifetime> _lifetimes;
        private readonly IDictionary<Type, Func<ServiceProvider, object>> _resolveExpressions;
        private readonly IDictionary<Type, object> _addedInstances;

        private readonly ConcurrentDictionary<Type, Lazy<object>> _resolvedInstances;
        private readonly List<IDisposable> _scopedDisposables;
        private bool _scopeDisposed;

        /// <summary>
        /// Creates a new root provider.
        /// </summary>
        public ServiceProvider()
        {
            _serviceDefinitions = new Dictionary<Type, Type>();
            _lifetimes = new Dictionary<Type, ServiceLifetime>();
            _resolveExpressions = new Dictionary<Type, Func<ServiceProvider, object>>();
            _addedInstances = new Dictionary<Type, object>();
            _resolvedInstances = new ConcurrentDictionary<Type, Lazy<object>>();

            _resolveExpressions[typeof(IServiceScopeFactory)] = sp => sp.Root;
            _lifetimes[typeof(IServiceScopeFactory)] = ServiceLifetime.Singleton;
        }

        private ServiceProvider(ServiceProvider root)
        {
            _root = root;
            _resolvedInstances = new ConcurrentDictionary<Type, Lazy<object>>();
            _scopedDisposables = new List<IDisposable>();
        }

        private ServiceProvider Root => _root ?? this;
        private bool IsScope => _root != null;

        /// <summary>
        /// Creates a new <see cref="IServiceScope"/> rooted at the same registrations as this provider.
        /// </summary>
        public IServiceScope CreateScope()
        {
            ThrowIfScopeDisposed();
            return new ServiceScope(new ServiceProvider(Root));
        }

        /// <summary>
        /// Register type to resolve with the instance type to instantiate, using the Singleton lifetime.
        /// </summary>
        public void Add<TType, TImplementation>() where TImplementation : class, TType
        {
            Add<TType, TImplementation>(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Register type to resolve with the instance type to instantiate, using the specified <paramref name="lifetime"/>.
        /// </summary>
        public void Add<TType, TImplementation>(ServiceLifetime lifetime) where TImplementation : class, TType
        {
            Root.AddServiceDefinition(typeof(TType), typeof(TImplementation), lifetime);
        }

        /// <summary>
        /// Register type using the Singleton lifetime.
        /// </summary>
        public void Add<TType>()
        {
            Add<TType>(ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Register type using the specified <paramref name="lifetime"/>.
        /// </summary>
        public void Add<TType>(ServiceLifetime lifetime)
        {
            var type = typeof(TType);
            Root.AddServiceDefinition(type, type, lifetime);
        }

        /// <summary>
        /// Register type using the Singleton lifetime.
        /// </summary>
        public void Add(Type type)
        {
            Root.AddServiceDefinition(type, type, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Register type to resolve with the instance type to instantiate, using the Singleton lifetime.
        /// </summary>
        public void Add(Type type, Type typeImplementation)
        {
            Root.AddServiceDefinition(type, typeImplementation, ServiceLifetime.Singleton);
        }

        /// <summary>
        /// Register type to resolve with the instance type to instantiate, using the specified <paramref name="lifetime"/>.
        /// </summary>
        public void Add(Type type, Type typeImplementation, ServiceLifetime lifetime)
        {
            Root.AddServiceDefinition(type, typeImplementation, lifetime);
        }

        /// <summary>
        /// Register type with an expression that resolves instances. The expression is invoked on every resolve (Transient).
        /// </summary>
        public void Add<TType>(Func<ServiceProvider, object> provider)
        {
            Root.AddExpression(typeof(TType), provider);
        }

        /// <summary>
        /// Register type to resolve with a pre-built instance. The instance is shared by the root provider and all scopes.
        /// </summary>
        public void Add<TType>(object instance)
        {
            var rootProvider = Root;
            var type = typeof(TType);
            var instanceType = instance.GetType();
            rootProvider.AddServiceDefinition(type, instanceType, ServiceLifetime.Singleton);

            if (rootProvider._addedInstances.ContainsKey(instanceType))
            {
                rootProvider._addedInstances[instanceType] = instance;
            }
            else
            {
                rootProvider._addedInstances.Add(instanceType, instance);
            }
        }

        /// <summary>
        /// Get instance of a registered type.
        /// </summary>
        public TType Get<TType>() where TType : class
        {
            return ResolveType(typeof(TType), typeof(TType)) as TType;
        }

        /// <summary>
        /// Get instance of a registered type.
        /// </summary>
        public object Get(Type type)
        {
            return ResolveType(type, type);
        }

        /// <summary>
        /// Try to get an instance of a registered type without throwing when the type is not registered.
        /// </summary>
        public bool TryGet<TType>(out TType instance) where TType : class
        {
            if (Root.IsRegistered(typeof(TType)))
            {
                instance = ResolveType(typeof(TType), typeof(TType)) as TType;
                return true;
            }
            instance = null;
            return false;
        }

        /// <summary>
        /// Removes resolved instances from this provider's cache.
        /// </summary>
        public void Clear()
        {
            _resolvedInstances.Clear();
        }

        /// <summary>
        /// Removes resolved and (on the root provider) added instances from cache.
        /// </summary>
        public void Reset()
        {
            _resolvedInstances.Clear();
            if (!IsScope)
            {
                _addedInstances.Clear();
            }
        }

        internal void DisposeScopedInstances()
        {
            if (!IsScope)
            {
                return;
            }
            _scopeDisposed = true;

            List<IDisposable> snapshot;
            lock (_scopedDisposables)
            {
                snapshot = new List<IDisposable>(_scopedDisposables);
                _scopedDisposables.Clear();
            }
            _resolvedInstances.Clear();

            foreach (var disposable in snapshot)
            {
                disposable.Dispose();
            }
        }

        private void AddServiceDefinition(Type type, Type typeImplementation, ServiceLifetime lifetime)
        {
            if (_serviceDefinitions.ContainsKey(type))
            {
                _serviceDefinitions[type] = typeImplementation;
            }
            else
            {
                _serviceDefinitions.Add(type, typeImplementation);
            }
            _lifetimes[type] = lifetime;
        }

        private void AddExpression(Type type, Func<ServiceProvider, object> expression)
        {
            if (!_resolveExpressions.ContainsKey(type))
            {
                _resolveExpressions.Add(type, expression);
            }
        }

        private object ResolveType(Type typeToActivate, Type typeToResolve)
        {
            ThrowIfScopeDisposed();
            var rootProvider = Root;

            Type typeImplementation;
            if (typeToResolve.IsGenericType && !rootProvider._serviceDefinitions.ContainsKey(typeToResolve))
            {
                var genericTypeDefinition = typeToResolve.GetGenericTypeDefinition();
                var genericTypeImplementation = rootProvider.GetServiceDefinitionType(typeToActivate, genericTypeDefinition);
                typeImplementation = genericTypeImplementation.MakeGenericType(typeToResolve.GetGenericArguments());
            }
            else if (rootProvider._resolveExpressions.ContainsKey(typeToResolve))
            {
                return rootProvider._resolveExpressions[typeToResolve](this);
            }
            else
            {
                typeImplementation = rootProvider.GetServiceDefinitionType(typeToActivate, typeToResolve);
            }

            if (rootProvider._addedInstances.ContainsKey(typeImplementation))
            {
                return rootProvider._addedInstances[typeImplementation];
            }

            var lifetime = rootProvider.GetLifetime(typeToResolve, typeImplementation);

            switch (lifetime)
            {
                case ServiceLifetime.Singleton:
                    return GetOrCreateCached(rootProvider, typeToResolve, typeImplementation);
                case ServiceLifetime.Scoped:
                    if (!IsScope)
                    {
                        throw new InvalidOperationException(
                            $"Cannot resolve scoped service '{typeToResolve.FullName}' from the root provider. Create a scope first.");
                    }
                    return GetOrCreateCached(this, typeToResolve, typeImplementation);
                case ServiceLifetime.Transient:
                    var instance = CreateInstance(typeImplementation, BuildConstructorArguments(typeToResolve, typeImplementation));
                    TrackScopedDisposable(instance);
                    return instance;
                default:
                    throw new InvalidOperationException($"Unsupported service lifetime '{lifetime}'.");
            }
        }

        private object GetOrCreateCached(ServiceProvider owner, Type typeToResolve, Type typeImplementation)
        {
            var lazy = owner._resolvedInstances.GetOrAdd(typeImplementation, t =>
                new Lazy<object>(() =>
                {
                    var args = BuildConstructorArguments(typeToResolve, t);
                    var instance = Activator.CreateInstance(t, args);
                    if (owner.IsScope)
                    {
                        owner.TrackScopedDisposable(instance);
                    }
                    return instance;
                }, System.Threading.LazyThreadSafetyMode.ExecutionAndPublication));
            return lazy.Value;
        }

        private object[] BuildConstructorArguments(Type typeToResolve, Type typeImplementation)
        {
            var constructorInfo = typeImplementation.GetConstructors().FirstOrDefault();
            if (constructorInfo == null || !constructorInfo.GetParameters().Any())
            {
                return new object[0];
            }

            var parameterInfos = constructorInfo.GetParameters();
            var args = new object[parameterInfos.Length];

            for (var index = 0; index < parameterInfos.Length; index++)
            {
                var parameterInfo = parameterInfos[index];
                var parameterType = parameterInfo.ParameterType;
                if (parameterInfo.HasDefaultValue && !Root.IsRegistered(parameterType))
                {
                    args[index] = parameterInfo.DefaultValue;
                }
                else
                {
                    args[index] = ResolveType(typeToResolve, parameterType);
                }
            }
            return args;
        }

        private object CreateInstance(Type type, object[] args)
        {
            return Activator.CreateInstance(type, args);
        }

        private void TrackScopedDisposable(object instance)
        {
            if (!IsScope || !(instance is IDisposable disposable))
            {
                return;
            }
            lock (_scopedDisposables)
            {
                _scopedDisposables.Add(disposable);
            }
        }

        private bool IsRegistered(Type type)
        {
            return _serviceDefinitions.ContainsKey(type)
                || _resolveExpressions.ContainsKey(type)
                || (type.IsGenericType && _serviceDefinitions.ContainsKey(type.GetGenericTypeDefinition()));
        }

        private ServiceLifetime GetLifetime(Type typeToResolve, Type typeImplementation)
        {
            if (_lifetimes.TryGetValue(typeToResolve, out var lifetime))
            {
                return lifetime;
            }
            if (typeToResolve.IsGenericType && _lifetimes.TryGetValue(typeToResolve.GetGenericTypeDefinition(), out lifetime))
            {
                return lifetime;
            }
            if (_lifetimes.TryGetValue(typeImplementation, out lifetime))
            {
                return lifetime;
            }
            return ServiceLifetime.Singleton;
        }

        private Type GetServiceDefinitionType(Type typeToActivate, Type typeToResolve)
        {
            if (!_serviceDefinitions.ContainsKey(typeToResolve))
            {
                var errorMessage = typeToActivate == typeToResolve ?
                    $"Unable to activate type '{typeToActivate.FullName}'." :
                    $"Unable to resolve type '{typeToResolve.FullName}' while attempting to activate '{typeToActivate.FullName}'.";
                throw new InvalidOperationException(errorMessage);
            }
            return _serviceDefinitions[typeToResolve];
        }

        private void ThrowIfScopeDisposed()
        {
            if (_scopeDisposed)
            {
                throw new ObjectDisposedException(nameof(ServiceProvider));
            }
        }
    }
}
