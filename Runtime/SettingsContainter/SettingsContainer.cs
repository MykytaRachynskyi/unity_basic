using Basic.Singleton;
using UnityEngine;

namespace Basic
{
    public class SettingsContainer<TContainer, TSettings>
        : Singleton<SettingsContainer<TContainer, TSettings>>
    {
        [SerializeField]
        protected TSettings settings;

        public static TSettings Settings
        {
            get { return Instance.settings; }
        }
    }
}
