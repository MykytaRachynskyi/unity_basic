using UnityEngine;

namespace Basic.Utility
{
    public class MonoBehaviourCallbackDispatcher : MonoBehaviour
    {
        public System.Action OnGUICallback;
        public System.Action OnDrawGizmosCallback;

        private void OnGUI() => OnGUICallback?.Invoke();

        private void OnDrawGizmos() => OnDrawGizmosCallback?.Invoke();
    }
}
