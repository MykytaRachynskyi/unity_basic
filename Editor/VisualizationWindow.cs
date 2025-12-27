using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Basic
{
    public class VisualizationWindow : EditorWindow
    {
        public delegate void DrawVisualizationDelegate(SceneView sceneView);
        public delegate void DrawGUIDelegate();

        private class ModuleEntry
        {
            public string name;
            public string description;
            public DrawVisualizationDelegate drawCallback;
            public DrawGUIDelegate guiCallback;
            public bool enabled;
            public bool foldout;
        }

        private static List<ModuleEntry> modules = new List<ModuleEntry>();
        private Vector2 scrollPosition;

        [MenuItem("Tools/Visualization Manager")]
        static void Init()
        {
            var window = GetWindow<VisualizationWindow>("Viz Manager");
            window.Show();
        }

        public static void RegisterModule(
            string moduleName,
            DrawVisualizationDelegate drawCallback,
            DrawGUIDelegate guiCallback = null,
            string description = "",
            bool enabledByDefault = true
        )
        {
            var entry = new ModuleEntry
            {
                name = moduleName,
                description = description,
                drawCallback = drawCallback,
                guiCallback = guiCallback,
                enabled = enabledByDefault,
                foldout = false,
            };
            modules.Add(entry);
        }

        public static void UnregisterModule(string moduleName)
        {
            modules.RemoveAll(m => m.name == moduleName);
        }

        private void OnEnable()
        {
            SceneView.duringSceneGui += OnSceneGUI;
        }

        private void OnDisable()
        {
            SceneView.duringSceneGui -= OnSceneGUI;
        }

        private void OnSceneGUI(SceneView sceneView)
        {
            foreach (var module in modules)
            {
                if (module.enabled)
                {
                    module.drawCallback?.Invoke(sceneView);
                }
            }
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Visualization Modules", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            if (modules.Count == 0)
            {
                EditorGUILayout.HelpBox("No modules registered yet.", MessageType.Info);
                return;
            }

            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            foreach (var module in modules)
            {
                EditorGUILayout.BeginVertical(EditorStyles.helpBox);

                // Header with checkbox and foldout
                EditorGUILayout.BeginHorizontal();

                bool newState = EditorGUILayout.Toggle(module.enabled, GUILayout.Width(20));

                if (newState != module.enabled)
                {
                    module.enabled = newState;
                    SceneView.RepaintAll();
                }

                // Only show foldout if module is enabled and has GUI callback
                if (module.enabled && module.guiCallback != null)
                {
                    module.foldout = EditorGUILayout.Foldout(module.foldout, module.name, true);
                }
                else
                {
                    EditorGUILayout.LabelField(module.name);
                }

                EditorGUILayout.EndHorizontal();

                // Description
                if (!string.IsNullOrEmpty(module.description))
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.LabelField(module.description, EditorStyles.miniLabel);
                    EditorGUI.indentLevel--;
                }

                // Module-specific GUI controls
                if (module.enabled && module.foldout && module.guiCallback != null)
                {
                    EditorGUI.indentLevel++;
                    EditorGUILayout.Space(5);

                    EditorGUILayout.BeginVertical(EditorStyles.helpBox);
                    module.guiCallback.Invoke();
                    EditorGUILayout.EndVertical();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
                EditorGUILayout.Space(5);
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.Space();
            if (GUILayout.Button("Refresh Scene View"))
            {
                SceneView.RepaintAll();
            }
        }
    }
}
