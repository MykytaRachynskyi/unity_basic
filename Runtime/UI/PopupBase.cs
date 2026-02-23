using Basic.Input;
using UnityEngine;

namespace Basic.UI
{
    public class PopupBase : MonoBehaviour
    {
        [SerializeField]
        protected GameObject mainGameObject;

        protected virtual InputRegion InputRegion => null;

        public virtual void Open()
        {
            mainGameObject.SetActive(true);
        }

        public virtual void Close()
        {
            mainGameObject.SetActive(false);
        }
    }
}
