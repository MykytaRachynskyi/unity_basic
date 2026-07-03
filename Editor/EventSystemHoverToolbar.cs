using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Toolbars;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UIElements;

namespace Basic.UnityEditorTools
{
	[InitializeOnLoad]
	public static class EventSystemHoverToolbar
	{
		public const string ToolbarPath = "Basic/EventSystemHover";

		private const int DockIndex = 102;

		private static GameObject _lastHovered;
		private static readonly List<RaycastResult> RaycastResults = new(8);

		public static GameObject CurrentHovered { get; private set; }

		static EventSystemHoverToolbar()
		{
			EditorApplication.update += OnEditorUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		[MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Right, defaultDockIndex = DockIndex)]
		private static MainToolbarElement CreateToolbarElement()
		{
			return new MainToolbarLabel(CreateContent())
			{
				populateContextMenu = PopulateContextMenu
			};
		}

		private static void OnPlayModeStateChanged(PlayModeStateChange state)
		{
			if (state != PlayModeStateChange.EnteredPlayMode && state != PlayModeStateChange.EnteredEditMode)
				return;

			_lastHovered = null;
			CurrentHovered = null;
			MainToolbar.Refresh(ToolbarPath);
		}

		private static void OnEditorUpdate()
		{
			if (!EditorApplication.isPlaying)
				return;

			var hovered = GetHoveredGameObject();
			CurrentHovered = hovered;

			if (hovered != _lastHovered)
			{
				_lastHovered = hovered;
				MainToolbar.Refresh(ToolbarPath);
			}

			if (EventSystemHoverSettings.IsHotkeyPressedThisFrame() && hovered != null)
			{
				Selection.activeGameObject = hovered;
				EditorGUIUtility.PingObject(hovered);
			}
		}

		private static MainToolbarContent CreateContent()
		{
			if (!EditorApplication.isPlaying)
				return new MainToolbarContent("Hover: —", "Not playing");

			if (EventSystem.current == null)
				return new MainToolbarContent("Hover: —", "No EventSystem in scene");

			if (CurrentHovered == null)
			{
				return new MainToolbarContent(
					"Hover: —",
					$"Nothing under mouse. {EventSystemHoverSettings.HotkeyDisplayString} to select hovered object."
				);
			}

			return new MainToolbarContent(
				$"Hover: {CurrentHovered.name}",
				$"{GetHierarchyPath(CurrentHovered)}\n{EventSystemHoverSettings.HotkeyDisplayString} to select in Hierarchy"
			);
		}

		private static GameObject GetHoveredGameObject()
		{
			var eventSystem = EventSystem.current;
			if (eventSystem == null)
				return null;

			RaycastResults.Clear();
			var pointerData = new PointerEventData(eventSystem) { position = EventSystemHoverInput.GetMouseScreenPosition() };
			eventSystem.RaycastAll(pointerData, RaycastResults);
			return RaycastResults.Count > 0 ? RaycastResults[0].gameObject : null;
		}

		private static string GetHierarchyPath(GameObject gameObject)
		{
			var path = gameObject.name;
			var current = gameObject.transform.parent;
			while (current != null)
			{
				path = current.name + "/" + path;
				current = current.parent;
			}

			return path;
		}

		private static void PopulateContextMenu(DropdownMenu menu)
		{
			menu.AppendAction(
				"Open Settings…",
				_ => SettingsService.OpenUserPreferences(EventSystemHoverSettings.PreferencesPath)
			);
		}
	}
}
