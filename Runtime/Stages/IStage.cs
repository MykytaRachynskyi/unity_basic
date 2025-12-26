using UnityEngine;

namespace Basic.Stages
{
    public interface IStage
    {
        public void Deinit();
        public void Update() { }
        public void LateUpdate() { }
        public void FixedUpdate() { }
        public void OnGUI() { }
        public void OnDrawGizmos() { }
    }
}
