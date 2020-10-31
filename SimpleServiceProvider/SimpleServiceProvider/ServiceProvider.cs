using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleServiceProvider
{
    /// <summary>
    /// Dependency injection provider
    /// </summary>
    public class ServiceProvider
    {
        private readonly IDictionary<Type, Type> _serviceDefinitions = new Dictionary<Type, Type>();
        private readonly IDictionary<Type, object> _resolvedInstances = new Dictionary<Type, object>();
        private readonly IDictionary<Type, object> _addedInstances = new Dictionary<Type, object>();
        private readonly IDictionary<Type, Func<ServiceProvider, object>> _resolveExpressions =
            new Dictionary<Type, Func<ServiceProvider, object>>();

        /// <summary>
        /// Register type to resolve with the instance type to instantiate. 
        /// </summary> 
        public void Add<TType, TImplementation>() where TImplementation : class, TType
        {
            AddServiceDefinition(typeof(TType), typeof(TImplementation));
        }

        /// <summary>
        /// Register type. 
        /// </summary> 
        public void Add<TType>()
        {
            var type = typeof(TType);
            AddServiceDefinition(type, type);
        }

        /// <summary>
        /// Register type. 
        /// </summary> 
        public void Add(Type type)
        {
            AddServiceDefinition(type, type);
        }

        /// <summary>
        /// Register type to resolve with the instance type to instantiate. 
        /// </summary> 
        public void Add(Type type, Type typeImplementation)
        {
            AddServiceDefinition(type, typeImplementation);
        }

        /// <summary>
        /// Register type with expression to resolve instance. 
        /// </summary> 
        public void Add<TType>(Func<ServiceProvider, object> provider)
        {
            AddExpression(typeof(TType), provider);
        }

        /// <summary>
        /// Register type to resolve with an instance. 
        /// </summary> 
        public void Add<TType>(object instance)
        {
            var type = typeof(TType);
            var instanceType = instance.GetType();
            AddServiceDefinition(type, instanceType);

            if (_addedInstances.ContainsKey(instanceType))
            {
                _addedInstances[instanceType] = instance;
            }
            else
            {
                _addedInstances.Add(instanceType, instance);
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
        /// Removes resolved instances from cache.
        /// </summary>
        public void Clear()
        {
            _resolvedInstances.Clear();
        }

        /// <summary>
        /// Removes resolved and added instances from cache.
        /// </summary>
        public void Reset()
        {
            _addedInstances.Clear();
            _resolvedInstances.Clear();
        }

        private void AddServiceDefinition(Type type, Type typeImplementation)
        {
            if (_serviceDefinitions.ContainsKey(type))
            {
                _serviceDefinitions[type] = typeImplementation;
            }
            else
            {
                _serviceDefinitions.Add(type, typeImplementation);
            }
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
            Type typeImplementation;
            if (typeToResolve.IsGenericType && !_serviceDefinitions.ContainsKey(typeToResolve))
            {
                var genericTypeDefinition = typeToResolve.GetGenericTypeDefinition();
                var genericTypeImplementation = GetServiceDefinitionType(typeToActivate, genericTypeDefinition);
                typeImplementation = genericTypeImplementation.MakeGenericType(typeToResolve.GetGenericArguments());
            }
            else if (_resolveExpressions.ContainsKey(typeToResolve))
            {
                return _resolveExpressions[typeToResolve](this);
            }
            else
            {
                typeImplementation = GetServiceDefinitionType(typeToActivate, typeToResolve);
            }

            if (_addedInstances.ContainsKey(typeImplementation))
            {
                return _addedInstances[typeImplementation];
            }

            if (_resolvedInstances.ContainsKey(typeImplementation))
            {
                return _resolvedInstances[typeImplementation];
            }

            var constructorInfo = typeImplementation.GetConstructors().FirstOrDefault();

            if (constructorInfo == null || !constructorInfo.GetParameters().Any())
            {
                return CreateInstance(typeImplementation);
            }

            var parameterInfos = constructorInfo.GetParameters();
            var resolvedInstances = new object[parameterInfos.Length];

            for (var index = 0; index < parameterInfos.Length; index++)
            {
                var parameterType = parameterInfos[index].ParameterType;
                resolvedInstances[index] = _resolvedInstances.ContainsKey(parameterType)
                    ? _resolvedInstances[parameterType]
                    : ResolveType(typeToResolve, parameterType);
            }

            return CreateInstance(typeImplementation, resolvedInstances);
        }

        private object CreateInstance(Type type, params object[] args)
        {
            var instance = Activator.CreateInstance(type, args);
            _resolvedInstances.Add(type, instance);
            return instance;
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
    }
}