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

		private const int DockIndex = 1;
		private const float LabelWidth = 220f;

		private static GameObject _lastHovered;
		private static readonly List<RaycastResult> RaycastResults = new(8);
		private static bool _styleScheduled;

		public static GameObject CurrentHovered { get; private set; }

		static EventSystemHoverToolbar()
		{
			EditorApplication.update += OnEditorUpdate;
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
		}

		[MainToolbarElement(ToolbarPath, defaultDockPosition = MainToolbarDockPosition.Middle, defaultDockIndex = DockIndex)]
		private static MainToolbarElement CreateToolbarElement()
		{
			ScheduleApplyFixedWidthStyle();

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
			RefreshToolbar();
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
				RefreshToolbar();
			}

			if (EventSystemHoverSettings.IsHotkeyPressedThisFrame() && hovered != null)
			{
				Selection.activeGameObject = hovered;
				EditorGUIUtility.PingObject(hovered);
			}
		}

		private static void RefreshToolbar()
		{
			MainToolbar.Refresh(ToolbarPath);
			ScheduleApplyFixedWidthStyle();
		}

		private static void ScheduleApplyFixedWidthStyle()
		{
			if (_styleScheduled)
				return;

			_styleScheduled = true;
			EditorApplication.delayCall += ApplyFixedWidthStyle;
		}

		private static void ApplyFixedWidthStyle()
		{
			_styleScheduled = false;

			var element = FindToolbarVisualElement();
			if (element == null)
				return;

			element.style.flexShrink = 0;
			element.style.flexGrow = 0;
			element.style.width = LabelWidth;
			element.style.minWidth = LabelWidth;
			element.style.maxWidth = LabelWidth;

			var label = element.Q<Label>();
			if (label == null)
				return;

			label.style.flexShrink = 0;
			label.style.width = Length.Percent(100);
			label.style.overflow = Overflow.Hidden;
			label.style.textOverflow = TextOverflow.Ellipsis;
			label.style.unityTextOverflowPosition = TextOverflowPosition.End;
			label.style.whiteSpace = WhiteSpace.NoWrap;
		}

		private static VisualElement FindToolbarVisualElement()
		{
			foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				var root = window.rootVisualElement;
				if (root == null)
					continue;

				var element = root.Q<VisualElement>(ToolbarPath);
				if (element != null)
					return element;

				element = root.Query<VisualElement>()
					.Where(e => e.name == ToolbarPath || e.name.EndsWith("EventSystemHover"))
					.First();
				if (element != null)
					return element;
			}

			return null;
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
