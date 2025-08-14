using UnityEngine;

namespace Basic.Modules
{
    public interface IExternalModulesProvider
    {
        T Get<T>()
            where T : GameModule, new();
    }

    public abstract class GameModule
    {
        public IExternalModulesProvider ExternalModulesProvider { private get; set; }

        protected T External<T>()
            where T : GameModule, new()
        {
            return ExternalModulesProvider.Get<T>();
        }

        public abstract void Deinit();

        public virtual void Update() { }

        public virtual void LateUpdate() { }

        public virtual void OnGUI() { }

        public virtual void OnDrawGizmos() { }
    }
}
