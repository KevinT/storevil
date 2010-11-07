﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using StorEvil.Context;

namespace StorEvil.Interpreter
{
    // TODO! right now this depends on static variables to store the extensions methods
    // should convert this to participate in the normal IoC and just be injected into every place that needs it!
    public class ExtensionMethodHandler : IExtensionMethodHandler
    {
        private readonly List<MethodInfo> _allExtensionMethods = new List<MethodInfo>();

        private readonly List<Type> _addedTypes = new List<Type>();

        public ExtensionMethodHandler(AssemblyRegistry assemblyRegistry)
        {
            foreach (var type in assemblyRegistry.GetStaticClasses())
                AddExtensionMethods(type);
        }

        private void AddExtensionMethods(Type type)
        {
            if (AlreadyAddedType(type))
                return;
            _addedTypes.Add(type);

            var publicStaticMethods = type.GetMethods(BindingFlags.Public | BindingFlags.Static);
            var extensionMethods =
                publicStaticMethods.Where(methodInfo => methodInfo.IsStatic & methodInfo.IsDefined(typeof (ExtensionAttribute), true));
            
            foreach (var methodInfo in extensionMethods)
            {
                DebugTrace.Trace("ExtensionMethodHandler", "Adding extension method: " + type.Name + "." + methodInfo.Name);
                _allExtensionMethods.Add(methodInfo);
            }
        }

        private bool AlreadyAddedType(Type type)
        {
            return _addedTypes.Contains(type);
        }

        public  IEnumerable<MethodInfo> GetExtensionMethodsFor(Type t)
        {
            return _allExtensionMethods.Where(m => m.GetParameters()[0].ParameterType.IsAssignableFrom(t));
        }     
    }

    public interface IExtensionMethodHandler
    {
        IEnumerable<MethodInfo> GetExtensionMethodsFor(Type t);
    }
}