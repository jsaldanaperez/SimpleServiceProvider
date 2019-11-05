﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace SimpleServiceProvider
{
    public class ServiceProvider
    {
        private readonly IDictionary<Type, Type> _serviceDefinitions = new Dictionary<Type, Type>();
        private readonly IDictionary<Type, object> _instances = new Dictionary<Type, object>();
        
        public void Add<TType, TImplementation>() where TImplementation : class, TType
        {
            AddServiceDefinition(typeof(TType), typeof(TImplementation));
        }

        public void Add<TType>(object instance)
        {
            var type = typeof(TType);
            var instanceType = instance.GetType();
            AddServiceDefinition(type, instanceType);

            if (_instances.ContainsKey(instanceType))
            {
                _instances[instanceType] = instance;
            }
            else
            {
                _instances.Add(instanceType, instance);
            }
        }

        public TType Get<TType>() where TType : class
        {
            return ResolveType(typeof(TType)) as TType;
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
            var typeImplementation = _serviceDefinitions[type];
            if (_instances.ContainsKey(typeImplementation))
            {
                return _instances[typeImplementation];
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
                resolvedInstances[index] = _instances.ContainsKey(parameterType)
                    ? _instances[parameterType]
                    : ResolveType(parameterType);
            }

            return CreateInstance(typeImplementation, resolvedInstances);
        }

        private object CreateInstance(Type type, params object[] args)
        {
            var instance = Activator.CreateInstance(type, args);
            _instances.Add(type, instance);
            return instance;
        }
    }
}