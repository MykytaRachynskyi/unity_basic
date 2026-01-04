using System.Collections.Generic;
using Basic.Singleton;
using UnityEngine;

namespace Basic
{
    public abstract class Database<T, TConfig> : Singleton<T>
        where T : Database<T, TConfig>
        where TConfig : IConfig
    {
        protected abstract IList<TConfig> Configs { get; }

        private Dictionary<GUID, TConfig> _configMap;
        private HashSet<GUID> _usedGUIDs;

        public TConfig this[GUIDBasedConfigID index]
        {
            get
            {
                foreach (var item in Configs)
                {
                    if (item.ConfigID == index)
                    {
                        return item;
                    }
                }
                return default;
            }
        }

        protected virtual void OnValidate()
        {
            _usedGUIDs ??= new();
            _usedGUIDs.Clear();

            bool dirty = false;
            foreach (var config in Configs)
            {
                if (config.ConfigID.GUID == default || _usedGUIDs.Contains(config.ConfigID.GUID))
                {
                    Debug.Log($"Generating new ID for config {config.DEBUG_Name} in {name}!");
                    config.EDITOR_SetGUID(GUID.Generate());
                    dirty = true;
                }

                _usedGUIDs.Add(config.ConfigID.GUID);
            }

            if (dirty)
            {
#if UNITY_EDITOR
                UnityEditor.EditorUtility.SetDirty(this);
#endif
            }
        }

        public void GetNames(IList<string> list)
        {
            if (list == null)
                return;
            if (Configs == null)
                return;

            foreach (var config in Configs)
            {
                list.Add(config.DEBUG_Name);
            }
        }

        public string GUIDToName(GUID guid)
        {
            if (!EnsureConfigMap())
                return default;
            if (_configMap.TryGetValue(guid, out var config))
                return config.DEBUG_Name;
            return null;
        }

        public int GUIDToIndex(GUID guid)
        {
            if (!EnsureConfigMap())
                return -1;
            if (_configMap.TryGetValue(guid, out var config))
                return Configs.IndexOf(config);
            return -1;
        }

        public GUID IndexToGUID(int target) => Configs[target].ConfigID.GUID;

        public string IndexToName(int target) => Configs[target].DEBUG_Name;

        public IConfig IndexToConfig(int newIndex) => Configs[newIndex];

        private bool EnsureConfigMap()
        {
            if (Configs == null)
                return false;

            if (_configMap == null || _configMap.Count != Configs.Count)
            {
                _configMap = new Dictionary<GUID, TConfig>();
                foreach (var config in Configs)
                {
                    _configMap[config.ConfigID.GUID] = config;
                }
            }

            return true;
        }
    }
}
