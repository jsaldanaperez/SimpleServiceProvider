using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleServiceProvider
{
    public class ServiceProvider
    {
        private readonly IDictionary<Type, Type> _serviceDefinitions = new Dictionary<Type, Type>();
        private readonly IDictionary<Type, object> _instances = new Dictionary<Type, object>();
        
        public void Add<TInterface, TImplementation>() where TImplementation : class, TInterface
        {
            if (_serviceDefinitions.ContainsKey(typeof(TInterface)))
            {
                _serviceDefinitions[typeof(TInterface)] = typeof(TImplementation);
            }
            else
            {
                _serviceDefinitions.Add(typeof(TInterface), typeof(TImplementation));
            }
        }

        public void Add<TInterface>(object instance)
        {
            if (_instances.ContainsKey(typeof(TInterface)))
            {
                _instances[typeof(TInterface)] = instance;
            }
            else
            {
                _instances.Add(typeof(TInterface), instance);
            }
        }

        public TService GetService<TService>() where TService : class
        {
            return ResolveType(typeof(TService)) as TService;
        }

        private object ResolveType(Type interfaceType)
        { 
            if (_instances.ContainsKey(interfaceType))
            {
                return _instances[interfaceType];
            }

            var implementationType = _serviceDefinitions[interfaceType];
            var constructorInfo = implementationType.GetConstructors().FirstOrDefault();

            if (constructorInfo == null || !constructorInfo.GetParameters().Any())
            {
                var instance = Activator.CreateInstance(implementationType);
                _instances.Add(interfaceType, instance);
                return instance;
            }
 
            var resolvedInstances = new List<object>();
            constructorInfo.GetParameters().ToList().ForEach(x =>
            {
                var parameterType = x.ParameterType;
                resolvedInstances.Add(_instances.ContainsKey(parameterType) ? 
                     _instances[parameterType] : ResolveType(parameterType));
            });

            var newInstance = Activator.CreateInstance(implementationType, resolvedInstances.ToArray());
            _instances.Add(interfaceType, newInstance);

            return newInstance;
        } 
    }
}