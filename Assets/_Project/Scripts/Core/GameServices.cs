using System;
using System.Collections.Generic;

namespace MidnightLegacy
{
    /// <summary>
    /// Small service registry. Created in the bootstrap, used instead of scattered singletons.
    /// </summary>
    public static class GameServices
    {
        static readonly Dictionary<Type, object> services = new Dictionary<Type, object>();

        public static void Register<T>(T service) where T : class
        {
            services[typeof(T)] = service;
        }

        public static T Get<T>() where T : class
        {
            object s;
            return services.TryGetValue(typeof(T), out s) ? (T)s : null;
        }

        public static bool Has<T>() where T : class
        {
            return services.ContainsKey(typeof(T));
        }

        public static void Clear()
        {
            services.Clear();
        }
    }
}
