using Basic.Singleton;
using UnityEngine;

namespace Basic
{
    public class SettingsContainer<TContainer, TSettings> : Singleton<TContainer>
        where TContainer : SettingsContainer<TContainer, TSettings>
    {
        [SerializeField, InlineProperty]
        protected TSettings settings;

        public static TSettings Settings
        {
            get { return Instance.settings; }
        }
    }
}
