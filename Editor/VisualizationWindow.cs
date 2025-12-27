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

			private string EnabledKey => $"VizModule_{name}_Enabled";
			private string FoldoutKey => $"VizModule_{name}_Foldout";

			public void LoadState()
			{
				enabled = EditorPrefs.GetBool(EnabledKey, enabled);
				foldout = EditorPrefs.GetBool(FoldoutKey, foldout);
			}

			public void SaveState()
			{
				EditorPrefs.SetBool(EnabledKey, enabled);
				EditorPrefs.SetBool(FoldoutKey, foldout);
			}
		}

		private static readonly List<ModuleEntry> _modules = new();
		private Vector2 _scrollPosition;

		[MenuItem("Tools/Basic/Editor Visualizations")]
		static void Init()
		{
			var window = GetWindow<VisualizationWindow>("Editor Visualizations");
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

			// Load saved state if it exists
			entry.LoadState();

			_modules.Add(entry);
		}

		public static void UnregisterModule(string moduleName)
		{
			_modules.RemoveAll(m => m.name == moduleName);
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
			foreach (var module in _modules)
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

			if (_modules.Count == 0)
			{
				EditorGUILayout.HelpBox("No modules registered yet.", MessageType.Info);
				return;
			}

			_scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);

			foreach (var module in _modules)
			{
				EditorGUILayout.BeginVertical(EditorStyles.helpBox);

				EditorGUILayout.BeginHorizontal();

				bool newState = EditorGUILayout.Toggle(module.enabled, GUILayout.Width(20));

				if (newState != module.enabled)
				{
					module.enabled = newState;
					module.SaveState();
					SceneView.RepaintAll();
				}

				if (module.enabled && module.guiCallback != null)
				{
					bool newFoldout = EditorGUILayout.Foldout(module.foldout, module.name, true);
					if (newFoldout != module.foldout)
					{
						module.foldout = newFoldout;
						module.SaveState();
					}
				}
				else
				{
					EditorGUILayout.LabelField(module.name);
				}

				EditorGUILayout.EndHorizontal();

				if (!string.IsNullOrEmpty(module.description))
				{
					EditorGUI.indentLevel++;
					EditorGUILayout.LabelField(module.description, EditorStyles.miniLabel);
					EditorGUI.indentLevel--;
				}

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
