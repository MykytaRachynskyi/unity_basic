#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Basic.Singleton;
using UnityEditor;
using UnityEngine;

namespace Basic.Singleton.Editor
{
    public class ScriptableSingletonEditorWindow : EditorWindow
    {
        private ScriptableSingletonDatabase _database;
        private SerializedObject _serializedDatabase;
        private SerializedProperty _allSingletonsProperty;

        private Vector2 _listScrollPosition;
        private Vector2 _inspectorScrollPosition;

        [SerializeField]
        private int _selectedIndex = -1;

        [SerializeField]
        private Object _selectedSingleton;

        private UnityEditor.Editor _currentEditor;

        private const string GroupFoldoutPrefsPrefix = "ScriptableSingletonEditor.Group.";
        private const string UngroupedSectionName = "Ungrouped";

        [MenuItem("Window/Scriptable Singleton Editor")]
        public static void ShowWindow()
        {
            var window = GetWindow<ScriptableSingletonEditorWindow>("Singleton Editor");
            window.minSize = new Vector2(600, 400);
        }

        private void OnEnable()
        {
            LoadDatabase();
            RestoreEditorIfNeeded();
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        private void OnDisable()
        {
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
            CleanupEditor();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Restore the editor after entering/exiting play mode
            if (
                state == PlayModeStateChange.EnteredEditMode
                || state == PlayModeStateChange.EnteredPlayMode
            )
            {
                RestoreEditorIfNeeded();
            }
        }

        private void RestoreEditorIfNeeded()
        {
            if (_selectedSingleton != null && _currentEditor == null)
            {
                _currentEditor = UnityEditor.Editor.CreateEditor(_selectedSingleton);
            }
        }

        private void CleanupEditor()
        {
            if (_currentEditor != null)
            {
                DestroyImmediate(_currentEditor);
                _currentEditor = null;
            }
        }

        private void LoadDatabase()
        {
            // Try to load the database from the asset database
            var guids = AssetDatabase.FindAssets($"t:{nameof(ScriptableSingletonDatabase)}");
            if (guids.Length > 0)
            {
                var path = AssetDatabase.GUIDToAssetPath(guids[0]);
                _database = AssetDatabase.LoadAssetAtPath<ScriptableSingletonDatabase>(path);

                if (_database != null)
                {
                    _serializedDatabase = new SerializedObject(_database);
                    _allSingletonsProperty = _serializedDatabase.FindProperty("allSingletons");
                }
            }
        }

        private void OnGUI()
        {
            if (_database == null)
            {
                EditorGUILayout.HelpBox(
                    "Scriptable Singleton Database not found!",
                    MessageType.Error
                );
                if (GUILayout.Button("Refresh"))
                {
                    LoadDatabase();
                }
                return;
            }

            _serializedDatabase.Update();

            // Ensure editor is restored if needed (handles recompilation)
            RestoreEditorIfNeeded();

            EditorGUILayout.BeginHorizontal();

            // Left panel - List of singletons
            DrawSingletonList();

            // Right panel - Inspector
            DrawInspector();

            EditorGUILayout.EndHorizontal();

            _serializedDatabase.ApplyModifiedProperties();
        }

        private void DrawSingletonList()
        {
            EditorGUILayout.BeginVertical(GUILayout.Width(250));

            // Header
            EditorGUILayout.LabelField("Singletons", EditorStyles.boldLabel);

            // Refresh button
            if (GUILayout.Button("Refresh Database"))
            {
                _database
                    .GetType()
                    .GetMethod("RefreshDatabase", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.Invoke(_database, null);
                _serializedDatabase.Update();
                _selectedIndex = -1;
                _selectedSingleton = null;
                CleanupEditor();
            }

            EditorGUILayout.Space(5);

            // Scrollable list
            _listScrollPosition = EditorGUILayout.BeginScrollView(_listScrollPosition);

            var groupedIndices = BuildGroupedIndices();
            foreach (var groupName in ScriptableSingletonDatabase.GetGroups())
            {
                if (!groupedIndices.TryGetValue(groupName, out var indices))
                {
                    indices = new List<int>();
                }

                DrawGroupSection(groupName, indices);
            }

            if (
                groupedIndices.TryGetValue(UngroupedSectionName, out var ungroupedIndices)
                && ungroupedIndices.Count > 0
            )
            {
                DrawGroupSection(UngroupedSectionName, ungroupedIndices);
            }

            EditorGUILayout.EndScrollView();
            EditorGUILayout.EndVertical();
        }

        private Dictionary<string, List<int>> BuildGroupedIndices()
        {
            var groupedIndices = new Dictionary<string, List<int>>();
            var count = _allSingletonsProperty.arraySize;

            for (var i = 0; i < count; i++)
            {
                var element = _allSingletonsProperty.GetArrayElementAtIndex(i);
                var singleton = element.objectReferenceValue as Singleton;
                if (singleton == null)
                {
                    continue;
                }

                var groupName = string.IsNullOrEmpty(singleton.Group)
                    ? UngroupedSectionName
                    : singleton.Group;

                if (!groupedIndices.TryGetValue(groupName, out var indices))
                {
                    indices = new List<int>();
                    groupedIndices[groupName] = indices;
                }

                indices.Add(i);
            }

            return groupedIndices;
        }

        private void DrawGroupSection(string groupName, List<int> indices)
        {
            var foldoutKey = GroupFoldoutPrefsPrefix + groupName;
            var foldout = EditorPrefs.GetBool(foldoutKey, true);
            var foldoutStyle = new GUIStyle(EditorStyles.foldout) { fontStyle = FontStyle.Bold };
            var newFoldout = EditorGUILayout.Foldout(foldout, groupName, true, foldoutStyle);
            if (newFoldout != foldout)
            {
                EditorPrefs.SetBool(foldoutKey, newFoldout);
            }

            if (!newFoldout)
            {
                return;
            }

            EditorGUI.indentLevel++;
            foreach (var index in indices)
            {
                var element = _allSingletonsProperty.GetArrayElementAtIndex(index);
                var singleton = element.objectReferenceValue;
                if (singleton == null)
                {
                    continue;
                }

                var isSelected = index == _selectedIndex;
                var style = new GUIStyle(GUI.skin.button)
                {
                    alignment = TextAnchor.MiddleLeft,
                    normal = { background = isSelected ? Texture2D.grayTexture : null },
                };

                if (GUILayout.Button(singleton.name, style, GUILayout.Height(25)))
                {
                    SelectSingleton(index);
                }
            }

            EditorGUI.indentLevel--;
            EditorGUILayout.Space(5);
        }

        private void DrawInspector()
        {
            EditorGUILayout.BeginVertical();

            if (_selectedIndex >= 0 && _selectedIndex < _allSingletonsProperty.arraySize)
            {
                var element = _allSingletonsProperty.GetArrayElementAtIndex(_selectedIndex);
                var singleton = element.objectReferenceValue;

                if (singleton != null)
                {
                    // Header
                    EditorGUILayout.LabelField(singleton.name, EditorStyles.boldLabel);
                    EditorGUILayout.Space(5);

                    // Inspector scroll view
                    _inspectorScrollPosition = EditorGUILayout.BeginScrollView(
                        _inspectorScrollPosition
                    );

                    if (_currentEditor != null)
                    {
                        _currentEditor.OnInspectorGUI();
                    }

                    EditorGUILayout.EndScrollView();
                }
            }
            else
            {
                EditorGUILayout.HelpBox(
                    "Select a singleton from the list to view its inspector.",
                    MessageType.Info
                );
            }

            EditorGUILayout.EndVertical();
        }

        private void SelectSingleton(int index)
        {
            if (_selectedIndex == index)
                return;

            _selectedIndex = index;

            // Cleanup previous editor
            CleanupEditor();

            // Store and create new editor for selected singleton
            if (_selectedIndex >= 0 && _selectedIndex < _allSingletonsProperty.arraySize)
            {
                var element = _allSingletonsProperty.GetArrayElementAtIndex(_selectedIndex);
                _selectedSingleton = element.objectReferenceValue;

                if (_selectedSingleton != null)
                {
                    _currentEditor = UnityEditor.Editor.CreateEditor(_selectedSingleton);
                }
            }
            else
            {
                _selectedSingleton = null;
            }

            _inspectorScrollPosition = Vector2.zero;
        }
    }
}
#endif
