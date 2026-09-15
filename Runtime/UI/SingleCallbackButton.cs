using UnityEngine;
using UnityEngine.UI;

namespace Basic.UI
{
    public class SingleCallbackButton : MonoBehaviour
    {
        [SerializeField]
        protected Button mainButton;
        protected System.Action _callback;

        private void Awake() => mainButton.onClick.AddListener(OnClick);

        private void OnDestroy() => mainButton.onClick.RemoveListener(OnClick);

        public void Init(System.Action callback) => _callback = callback;

        private void OnClick() => _callback?.Invoke();
    }
}
