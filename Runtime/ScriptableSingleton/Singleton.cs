using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;

namespace Basic.Singleton
{
    public abstract class Singleton : ScriptableObject
    {
        [SerializeField]
        [Dropdown(nameof(GetGroupDropdownValues))]
        private string group;

        public string Group => group;

#if UNITY_EDITOR
        internal void ClearGroup()
        {
            group = string.Empty;
        }
#endif

        private List<string> GetGroupDropdownValues()
        {
            var values = new List<string> { string.Empty };
            values.AddRange(ScriptableSingletonDatabase.GetGroups());
            return values;
        }
    }

    public abstract class Singleton<T> : Singleton
        where T : Singleton<T>
    {
        private static T _instance;

        public static T Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ScriptableSingletonDatabase.GetSingleton<T>();
                    if (_instance == null)
                    {
                        ScriptableSingletonDatabase.Refresh();
                        _instance = ScriptableSingletonDatabase.GetSingleton<T>();
                    }
                }

                return _instance;
            }
        }
    }
}
