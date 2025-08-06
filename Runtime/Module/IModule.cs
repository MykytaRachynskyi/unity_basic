using UnityEngine;

namespace Basic.Modules
{
    public interface IModule
    {
        public void Deinit();
        public void Update() { }
        public void LateUpdate() { }
        public void OnGUI() { }
        public void OnDrawGizmos() { }
    }
}
