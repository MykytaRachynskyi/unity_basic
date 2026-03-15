using System.Collections.Generic;
using NaughtyAttributes;
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
                RecreateSingletonMap();
            }

            {
                if (!_singletonMap.TryGetValue(typeof(T).GetHashCode(), out var singleton))
                {
                    Log.Error($"Singleton of type {typeof(T).Name} not found in singleton map!");
                    return null;
                }

                return (T)singleton;
            }
        }

        private static void RecreateSingletonMap()
        {
            _singletonMap = new(Instance.allSingletons.Count);
            foreach (var singleton in Instance.allSingletons)
            {
                _singletonMap.TryAdd(singleton.GetType().GetHashCode(), singleton);
            }
        }

        [Button]
        private void RefreshDatabase()
        {
#if UNITY_EDITOR
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
                }
            }
            allSingletons.Sort((x, y) => x.name.CompareTo(y.name));
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);

            RecreateSingletonMap();
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
                            Log.Error(
                                "Failed to load Scriptable Singleton Database from asset database."
                            );
                        }
                    }
                    else
                    {
                        if (!LoadFromAddressables(out _instance))
                        {
                            Log.Error(
                                "Failed to load Scriptable Singleton Database from addressables."
                            );
                        }
                    }
                }

                return _instance;
            }
        }

        private static bool LoadFromAssetDatabase(out ScriptableSingletonDatabase instance)
#if UNITY_EDITOR
            =>
            TryLoadAssetFromAssetDatabase(out instance);
#else
        {
            instance = null;
            return false;
        }
#endif

        private static bool LoadFromAddressables(out ScriptableSingletonDatabase instance)
        {
            var op = Addressables.LoadAssetAsync<ScriptableSingletonDatabase>(
                typeof(ScriptableSingletonDatabase).Name
            );
            instance = op.WaitForCompletion();
            return instance != null;
        }

        private static bool TryLoadAssetFromAssetDatabase<T>(out T obj)
            where T : Object
        {
            obj = null;

#if UNITY_EDITOR
            var assetGUIDs = UnityEditor.AssetDatabase.FindAssets($"t: {typeof(T).Name}");
            if (assetGUIDs == null || assetGUIDs.Length == 0)
            {
                return false;
            }

            foreach (var guid in assetGUIDs)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                obj = UnityEditor.AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;

                if (obj != null)
                {
                    return true;
                }
            }
#endif

            return false;
        }
    }
}
