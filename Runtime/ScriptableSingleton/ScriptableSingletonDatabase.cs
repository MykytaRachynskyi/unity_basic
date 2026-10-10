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

        [SerializeField]
        private List<string> groups;

        private static Dictionary<int, Singleton> _singletonMap;
        private static bool _recreatingMap;

        public static IReadOnlyList<string> GetGroups()
        {
            var instance = Instance;
            if (instance == null || instance.groups == null)
            {
                return System.Array.Empty<string>();
            }

            return instance.groups;
        }

        public static void Refresh()
        {
            var instance = Instance;
            if (instance == null)
            {
                return;
            }

            instance.RefreshDatabase();
        }

        public static T GetSingleton<T>()
            where T : Singleton
        {
            if (_singletonMap == null)
            {
                RecreateSingletonMap();
            }

            if (!_singletonMap.TryGetValue(typeof(T).GetHashCode(), out var singleton))
            {
                if (!_recreatingMap)
                {
                    Log.Error($"Singleton of type {typeof(T).Name} not found in singleton map!");
                }

                return null;
            }

            return (T)singleton;
        }

        private static void RecreateSingletonMap()
        {
            if (_recreatingMap)
            {
                _singletonMap ??= new Dictionary<int, Singleton>();
                return;
            }

            _recreatingMap = true;
            try
            {
                // Ensure nested GetSingleton calls see a non-null map and do not re-enter.
                _singletonMap ??= new Dictionary<int, Singleton>();

                var db = Instance;
                if (db == null)
                {
                    _singletonMap = new Dictionary<int, Singleton>();
                    return;
                }

                _singletonMap = BuildSingletonMap(db.allSingletons);
            }
            finally
            {
                _recreatingMap = false;
            }
        }

        private static Dictionary<int, Singleton> BuildSingletonMap(List<Singleton> singletons)
        {
            if (singletons == null)
            {
                return new Dictionary<int, Singleton>();
            }

            var map = new Dictionary<int, Singleton>(singletons.Count);
            for (var i = 0; i < singletons.Count; i++)
            {
                var singleton = singletons[i];
                if (singleton == null)
                {
                    Debug.LogWarning(
                        $"ScriptableSingletonDatabase.allSingletons[{i}] is null and was skipped. "
                            + "The reference may be Editor-only or missing from the player build."
                    );
                    continue;
                }

                map.TryAdd(singleton.GetType().GetHashCode(), singleton);
            }

            return map;
        }

        [Button]
        private void RefreshDatabase()
        {
#if UNITY_EDITOR
            allSingletons ??= new();
            allSingletons.Clear();
            var playerAssemblyNames = new HashSet<string>();
            foreach (
                var assembly in UnityEditor.Compilation.CompilationPipeline.GetAssemblies(
                    UnityEditor.Compilation.AssembliesType.Player
                )
            )
            {
                playerAssemblyNames.Add(assembly.name);
            }

            var guids = UnityEditor.AssetDatabase.FindAssets("t: ScriptableObject");
            foreach (var guid in guids)
            {
                var path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                var scriptableObject = UnityEditor.AssetDatabase.LoadAssetAtPath<ScriptableObject>(
                    path
                );
                if (scriptableObject == null)
                {
                    continue;
                }

                var type = scriptableObject.GetType();

                if (!type.IsSubclassOf(typeof(Singleton)))
                {
                    continue;
                }

                var assemblyName = type.Assembly.GetName().Name;
                if (!playerAssemblyNames.Contains(assemblyName))
                {
                    Log.Warning(
                        $"Skipping Editor-only singleton '{type.Name}' at '{path}' (assembly '{assemblyName}' is not in player builds)."
                    );
                    continue;
                }

                allSingletons.Add(scriptableObject as Singleton);
            }
            allSingletons.Sort((x, y) => x.name.CompareTo(y.name));
            UnityEditor.EditorUtility.SetDirty(this);
            UnityEditor.AssetDatabase.SaveAssetIfDirty(this);

            RecreateSingletonMap();
#endif
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            if (allSingletons == null)
            {
                return;
            }

            var validGroups = groups != null ? new HashSet<string>(groups) : new HashSet<string>();

            foreach (var singleton in allSingletons)
            {
                if (singleton == null)
                {
                    continue;
                }

                if (string.IsNullOrEmpty(singleton.Group))
                {
                    continue;
                }

                if (validGroups.Contains(singleton.Group))
                {
                    continue;
                }

                singleton.ClearGroup();
                UnityEditor.EditorUtility.SetDirty(singleton);
            }
        }
#endif

        private static ScriptableSingletonDatabase _instance;

        // When true, Instance stays null instead of auto-loading from AssetDatabase/Addressables.
        // Used by EditMode tests that simulate a missing database in projects that ship one.
        private static bool _suppressAutoLoad;

        // Retained for process lifetime — releasing unloads the bundle and nulls nested
        // serialized refs on singletons (e.g. BuildingDatabase.allConfigs).
        private static AsyncOperationHandle<IList<ScriptableSingletonDatabase>> _addressablesHandle;
        private static ScriptableSingletonDatabase Instance
        {
            get
            {
                if (_instance == null)
                {
                    if (_suppressAutoLoad)
                    {
                        return null;
                    }

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
                Debug.LogWarning(
                    $"Multiple ScriptableSingletonDatabase assets found with label '{label}'; using '{assets[0].name}'."
                );
            }

            instance = assets[0];
            return instance != null;
        }

        private static bool TryLoadAssetFromAssetDatabase<T>(out T obj)
            where T : UnityEngine.Object
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
