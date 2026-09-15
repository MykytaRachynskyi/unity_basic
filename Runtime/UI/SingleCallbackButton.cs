using UnityEngine;
using UnityEngine.UI;

namespace Basic.UI
{
    public class SingleCallbackButton : MonoBehaviour
    {
        [SerializeField]
        protected Button mainButton;
        protected System.Action _callback;

        protected virtual void Awake() => mainButton.onClick.AddListener(OnClick);

        protected virtual void OnDestroy() => mainButton.onClick.RemoveListener(OnClick);

        public virtual void Init(System.Action callback) => _callback = callback;

        protected virtual void OnClick() => _callback?.Invoke();
    }
}
