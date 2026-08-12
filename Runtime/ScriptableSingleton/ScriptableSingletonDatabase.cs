using System.Collections.Generic;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

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

        // Retained for process lifetime — releasing unloads the bundle and nulls nested
        // serialized refs on singletons (e.g. BuildingDatabase.allConfigs).
        private static AsyncOperationHandle<IList<ScriptableSingletonDatabase>> _addressablesHandle;
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
                                $"Failed to load ScriptableSingletonDatabase from Addressables (label: {typeof(ScriptableSingletonDatabase).Name}). "
                                    + "Ensure the asset is addressable, labeled, and Addressables content is built."
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

        // Player builds load by Addressables label (not address). Consuming projects must:
        // - Mark ScriptableSingletonDatabase.asset as Addressable
        // - Assign label ScriptableSingletonDatabase (typeof(ScriptableSingletonDatabase).Name)
        // - Address may remain the default asset path
        // - Build Addressables content with the player
        // - Prefer recursive dependency building (NonRecursiveBuilding = false) so nested
        //   singleton assets (databases, configs) are included in bundles
        // Exactly one asset should carry this label.
        private static bool LoadFromAddressables(out ScriptableSingletonDatabase instance)
        {
            instance = null;
            var label = typeof(ScriptableSingletonDatabase).Name;
            if (!_addressablesHandle.IsValid())
            {
                _addressablesHandle = Addressables.LoadAssetsAsync<ScriptableSingletonDatabase>(
                    label,
                    _ => { }
                );
            }

            var assets = _addressablesHandle.WaitForCompletion();
            if (assets == null || assets.Count == 0)
            {
                return false;
            }

            if (assets.Count > 1)
            {
                Log.Warning(
                    $"Multiple ScriptableSingletonDatabase assets found with label '{label}'; using '{assets[0].name}'."
                );
            }

            instance = assets[0];
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
