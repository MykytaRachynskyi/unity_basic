using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Basic.Modules
{
    public class ModuleTable : IEnumerable<KeyValuePair<int, GameModule>>
    {
        private IExternalModulesProvider _provider;
        private readonly Dictionary<int, GameModule> _modules = new();

        public ModuleTable(IExternalModulesProvider provider) => _provider = provider;

        public T Add<T>(T module)
            where T : GameModule
        {
            if (!_modules.TryAdd(typeof(T).GetHashCode(), module))
            {
                Debug.LogError($"Couldn't add module {typeof(T).Name} to the module table.");
                return null;
            }

            module.ExternalModulesProvider = _provider;
            return module;
        }

        public T Get<T>()
            where T : GameModule
        {
            if (_modules.TryGetValue(typeof(T).GetHashCode(), out var module))
            {
                return (T)module;
            }
            return null;
        }

        public void Clear() => _modules.Clear();

        public IEnumerator<KeyValuePair<int, GameModule>> GetEnumerator()
        {
            return _modules.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
