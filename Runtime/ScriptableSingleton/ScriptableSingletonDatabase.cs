using System.Collections.Generic;
using Basic.UnityEditorTools;
using NaughtyAttributes;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace Basic.Singleton
{
    [CreateAssetMenu(
        fileName = "ScriptableSingletonDatabase",
        menuName = "Basic/ScriptableSingleton/Database"
    )]
    public class ScriptableSingletonDatabase : ScriptableObject
    {
        [SerializeField]
        private List<Singleton> allSingletons;

        private static Dictionary<int, Singleton> _singletonMap;

        public static void Refresh()
        {
            Instance.RefreshDatabase();
        }

        public static T GetSingleton<T>()
            where T : Singleton
        {
            if (_singletonMap == null)
            {
                _singletonMap = new(Instance.allSingletons.Count);
                foreach (var singleton in Instance.allSingletons)
                {
                    _singletonMap.Add(singleton.GetType().GetHashCode(), singleton);
                }
            }

            {
                if (!_singletonMap.TryGetValue(typeof(T).GetHashCode(), out var singleton))
                {
                    Debug.LogError(
                        $"Singleton of type {typeof(T).Name} not found in singleton map!"
                    );
                    return null;
                }

                return (T)singleton;
            }
        }

        [Button]
        private void RefreshDatabase()
        {
#if UNITY_EDITOR
            AddressableAssetSettings settings = AddressableAssetSettingsDefaultObject.Settings;

            allSingletons ??= new();
            allSingletons.Clear();
            var guids = UnityEditor.AssetDatabase.FindAssets("t: ScriptableObject");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var scriptableObject = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    path
                );
                var type = scriptableObject.GetType();

                if (type.IsSubclassOf(typeof(Singleton)))
                {
                    allSingletons.Add(scriptableObject as Singleton);
                    AddressableAssetEntry entry = settings.FindAssetEntry(guid);

                    if (entry != null)
                    {
                        settings.RemoveAssetEntry(guid, true);
                        settings.SetDirty(
                            AddressableAssetSettings.ModificationEvent.EntryRemoved,
                            entry,
                            true
                        );
                    }
                }
            }
            allSingletons.Sort((x, y) => x.name.CompareTo(y.name));
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);
#endif
        }

        private static ScriptableSingletonDatabase _instance;
        private static ScriptableSingletonDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (Application.isEditor)
                    {
                        if (!LoadFromAssetDatabase(out _instance))
                        {
                            Debug.LogError(
                                "Failed to load Scriptable Singleton Database from asset database."
                            );
                        }
                    }
                    else
                    {
                        if (!LoadFromAddressables(out _instance))
                        {
                            Debug.LogError(
                                "Failed to load Scriptable Singleton Database from addressables."
                            );
                        }
                    }
                }

                return _instance;
            }
        }

        private static bool LoadFromAssetDatabase(out ScriptableSingletonDatabase instance) =>
            EditorTools.TryLoadAssetFromAssetDatabase(out instance);

        private static bool LoadFromAddressables(out ScriptableSingletonDatabase instance)
        {
            var op = Addressables.LoadAssetAsync<ScriptableSingletonDatabase>(
                typeof(ScriptableSingletonDatabase).Name
            );
            instance = op.WaitForCompletion();
            return instance != null;
        }
    }
}
