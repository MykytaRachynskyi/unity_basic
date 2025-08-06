using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Basic.Modules
{
    public class ModuleTable : IEnumerable<KeyValuePair<int, IModule>>
    {
        private readonly Dictionary<int, IModule> _modules = new();

        public void Add<T>(T module)
            where T : IModule
        {
            if (!_modules.TryAdd(typeof(T).GetHashCode(), module))
            {
                Debug.LogError($"Couldn't add module {typeof(T).Name} to the module table.");
                return;
            }
        }

        public T Get<T>()
            where T : IModule
        {
            if (_modules.TryGetValue(typeof(T).GetHashCode(), out var module))
            {
                return (T)module;
            }
            return default;
        }

        public void Clear() => _modules.Clear();

        public IEnumerator<KeyValuePair<int, IModule>> GetEnumerator()
        {
            return _modules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
