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
		private static GameObject _lastHoveredInGameView;
		private static readonly List<RaycastResult> RaycastResults = new(8);
		private static bool _styleScheduled;
		private static int _lastHoverUpdateFrame = -1;

		public static GameObject CurrentHovered { get; private set; }

		static EventSystemHoverToolbar()
		{
			EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
			if (EditorApplication.isPlaying)
				Application.onBeforeRender += OnBeforeRender;
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
			if (state == PlayModeStateChange.EnteredPlayMode)
				Application.onBeforeRender += OnBeforeRender;
			else if (state == PlayModeStateChange.ExitingPlayMode)
				Application.onBeforeRender -= OnBeforeRender;

			if (state != PlayModeStateChange.EnteredPlayMode && state != PlayModeStateChange.EnteredEditMode)
				return;

			_lastHovered = null;
			_lastHoveredInGameView = null;
			CurrentHovered = null;
			_lastHoverUpdateFrame = -1;
			RefreshToolbar();
		}

		private static void OnBeforeRender()
		{
			if (!EditorApplication.isPlaying || EventSystemHoverInput.ShouldPollInputSystemHotkey)
				return;

			UpdateHoverState();
		}

		internal static void UpdateHoverState()
		{
			if (!EditorApplication.isPlaying)
				return;

			if (_lastHoverUpdateFrame == Time.frameCount)
				return;

			_lastHoverUpdateFrame = Time.frameCount;

			GameObject hovered;
			if (IsMouseInGameView())
			{
				hovered = GetHoveredGameObject();
				if (hovered != null)
					_lastHoveredInGameView = hovered;
			}
			else
			{
				hovered = _lastHoveredInGameView;
			}

			CurrentHovered = hovered;

			if (hovered != _lastHovered)
			{
				_lastHovered = hovered;
				RefreshToolbar();
			}
		}

		private static bool IsMouseInGameView()
		{
			var mousePosition = EventSystemHoverInput.GetMouseScreenPosition();
			var screenPoint = new Vector2(mousePosition.x, Screen.height - mousePosition.y);

			foreach (var window in Resources.FindObjectsOfTypeAll<EditorWindow>())
			{
				if (window == null)
					continue;

				var typeName = window.GetType().Name;
				if (typeName != "GameView" && typeName != "SimulatorWindow")
					continue;

				if (window.position.Contains(screenPoint))
					return true;
			}

			return false;
		}

		public static void TrySelectHovered()
		{
			if (!EditorApplication.isPlaying || CurrentHovered == null)
				return;

			Selection.activeGameObject = CurrentHovered;
			EditorGUIUtility.PingObject(CurrentHovered);
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
				var idleTooltip = IsMouseInGameView()
					? $"Nothing under mouse. {EventSystemHoverSettings.HotkeyDisplayString} to select hovered object."
					: $"Mouse outside Game view. {EventSystemHoverSettings.HotkeyDisplayString} to select last hovered object.";

				return new MainToolbarContent("Hover: —", idleTooltip);
			}

			var tooltipPath = GetHierarchyPath(CurrentHovered);
			if (!IsMouseInGameView())
				tooltipPath += "\n(Mouse outside Game view — showing last hovered object)";

			return new MainToolbarContent(
				$"Hover: {CurrentHovered.name}",
				$"{tooltipPath}\n{EventSystemHoverSettings.HotkeyDisplayString} to select in Hierarchy"
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
