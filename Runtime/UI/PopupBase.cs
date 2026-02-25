using UnityEngine;

namespace Basic.UI
{
    public class PopupBase : MonoBehaviour
    {
        [SerializeField]
        protected GameObject mainGameObject;

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
