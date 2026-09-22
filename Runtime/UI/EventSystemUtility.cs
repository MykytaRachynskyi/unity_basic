using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Basic.UI
{
	public static class EventSystemUtility
	{
		private static readonly List<RaycastResult> RaycastResults = new(8);

		public static bool IsPointerOverGameObject() =>
			EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();

		public static bool IsPointerOverGameObject(out string scenePath)
		{
			scenePath = null;
			var eventSystem = EventSystem.current;
			if (eventSystem == null || !eventSystem.IsPointerOverGameObject())
				return false;

			var gameObject = RaycastPointerGameObject(eventSystem);
			if (gameObject != null)
				scenePath = GetScenePath(gameObject);

			return true;
		}

		public static string GetScenePath(GameObject gameObject)
		{
			if (gameObject == null)
				return null;

			var path = gameObject.name;
			var current = gameObject.transform.parent;
			while (current != null)
			{
				path = current.name + "/" + path;
				current = current.parent;
			}

			return path;
		}

		private static GameObject RaycastPointerGameObject(EventSystem eventSystem)
		{
			RaycastResults.Clear();
			var pointerData = new PointerEventData(eventSystem) { position = GetPointerScreenPosition() };
			eventSystem.RaycastAll(pointerData, RaycastResults);
			return RaycastResults.Count > 0 ? RaycastResults[0].gameObject : null;
		}

		private static Vector2 GetPointerScreenPosition()
		{
#if ENABLE_INPUT_SYSTEM
			var mouse = Mouse.current;
			if (mouse != null)
				return mouse.position.ReadValue();
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
			return UnityEngine.Input.mousePosition;
#else
			return Vector2.zero;
#endif
		}
	}
}
