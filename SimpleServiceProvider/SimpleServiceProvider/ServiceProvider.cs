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

        /// <summary>
        /// Register type to resolve with the instance type to instantiate. 
        /// </summary> 
        public void Add<TType, TImplementation>() where TImplementation : class, TType
        {
            AddServiceDefinition(typeof(TType), typeof(TImplementation));
        }
        
        /// <summary>
        /// Register type to resolve with the instance type to instantiate. 
        /// </summary> 
        public void Add(Type type, Type typeImplementation)
        {
            AddServiceDefinition(type, typeImplementation);
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
            return ResolveType(typeof(TType)) as TType;
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

        private object ResolveType(Type type)
        {
            Type typeImplementation;
            if (type.IsGenericType && !_serviceDefinitions.ContainsKey(type))
            {
                var genericTypeDefinition = type.GetGenericTypeDefinition();
                var genericTypeImplementation = _serviceDefinitions[genericTypeDefinition];
                typeImplementation = genericTypeImplementation.MakeGenericType(type.GetGenericArguments());
            }
            else
            {
                typeImplementation = _serviceDefinitions[type];
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
                    : ResolveType(parameterType);
            }

            return CreateInstance(typeImplementation, resolvedInstances);
        }

        private object CreateInstance(Type type, params object[] args)
        {
            var instance = Activator.CreateInstance(type, args);
            _resolvedInstances.Add(type, instance);
            return instance;
        }
    }
}