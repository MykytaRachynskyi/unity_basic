using UnityEngine;

namespace Basic.Utility
{
    public class DisableOnAwake : MonoBehaviour
    {
        private void Awake() => gameObject.SetActive(false);
    }
}
