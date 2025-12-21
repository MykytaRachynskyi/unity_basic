using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Basic.Modules
{
    public class ModuleTable : IEnumerable<KeyValuePair<int, GameModule>>
    {
        private List<IExternalModulesProvider> _providers;
        private readonly Dictionary<int, GameModule> _modules = new();

        public ModuleTable(IExternalModulesProvider provider)
        {
            _providers = new() { provider };
        }

        public void AddProvider(IExternalModulesProvider provider)
        {
            if (!_providers.Contains(provider))
            {
                _providers.Add(provider);
            }
            else
            {
                Debug.LogError("The provider has already been added to the module table.");
            }
        }

        public T Add<T>(T module)
            where T : GameModule
        {
            if (!_modules.TryAdd(typeof(T).GetHashCode(), module))
            {
                Debug.LogError($"Couldn't add module {typeof(T).Name} to the module table.");
                return null;
            }

            module.ExternalModulesProviders = _providers;
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
