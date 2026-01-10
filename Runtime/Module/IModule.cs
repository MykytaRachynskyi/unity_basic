using System.Collections.Generic;
using UnityEngine;

namespace Basic.Modules
{
    public interface IExternalModulesProvider
    {
        T Get<T>()
            where T : GameModule;
    }

    public abstract class GameModule
    {
        public List<IExternalModulesProvider> ExternalModulesProviders { private get; set; }

        protected T Get<T>()
            where T : GameModule
        {
            foreach (var provider in ExternalModulesProviders)
            {
                var module = provider.Get<T>();
                if (module != null)
                {
                    return module;
                }
            }
            return null;
        }

        public abstract void Deinit();

        public virtual void Update() { }

        public virtual void LateUpdate() { }

        public virtual void FixedUpdate(long tick) { }

        public virtual void OnGUI() { }

        public virtual void OnDrawGizmos() { }
    }
}
