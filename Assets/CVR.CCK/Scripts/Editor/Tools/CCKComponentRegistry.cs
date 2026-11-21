using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ABI.CCK.Components;
using UnityEngine;

namespace CVR.CCKEditor
{
    public static class CCKComponentRegistry
    {
        private static readonly HashSet<Type> _monoBehaviourComponentTypes = new();
        private static readonly HashSet<Type> _stateMachineBehaviourComponentTypes = new();

        static CCKComponentRegistry() => PopulateCCKComponentTypes();

        private static void PopulateCCKComponentTypes()
        {
            var allTypes = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a =>
                {
                    try { return a.GetTypes(); }
                    catch { return Array.Empty<Type>(); }
                })
                .Where(t => t.GetCustomAttribute<CVRComponentAttribute>() != null && !t.IsAbstract && t.IsClass);

            foreach (Type type in allTypes)
            {
                if (typeof(MonoBehaviour).IsAssignableFrom(type))
                    _monoBehaviourComponentTypes.Add(type);
                else if (typeof(StateMachineBehaviour).IsAssignableFrom(type)) 
                    _stateMachineBehaviourComponentTypes.Add(type);
            }
        }

        public static IEnumerable<Type> GetMonoBehaviourComponentTypes() 
            => _monoBehaviourComponentTypes;

        public static IEnumerable<Type> GetStateMachineBehaviourComponentTypes() 
            => _stateMachineBehaviourComponentTypes;
    }
}