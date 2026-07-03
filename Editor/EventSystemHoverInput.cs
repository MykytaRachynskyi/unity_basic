using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Basic.UnityEditorTools
{
	public static class EventSystemHoverInput
	{
		private const string ActiveInputHandlerProperty = "activeInputHandler";

		public static Vector2 GetMouseScreenPosition()
		{
#if ENABLE_INPUT_SYSTEM
			if (PreferInputSystem())
			{
				var mouse = Mouse.current;
				if (mouse != null)
					return mouse.position.ReadValue();
			}
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
			if (CanUseLegacyInput())
				return UnityEngine.Input.mousePosition;
#endif
			return Vector2.zero;
		}

		public static bool IsHotkeyPressedThisFrame(KeyCode key, ShortcutModifiers modifiers)
		{
			if (!IsKeyDown(key))
				return false;

			var shiftRequired = (modifiers & ShortcutModifiers.Shift) != 0;
			var altRequired = (modifiers & ShortcutModifiers.Alt) != 0;
			var actionRequired = (modifiers & ShortcutModifiers.Action) != 0;

			var shiftHeld = IsModifierHeld(ShortcutModifiers.Shift);
			var altHeld = IsModifierHeld(ShortcutModifiers.Alt);
			var actionHeld = IsModifierHeld(ShortcutModifiers.Action);

			return shiftHeld == shiftRequired
			       && altHeld == altRequired
			       && actionHeld == actionRequired;
		}

		private static bool IsKeyDown(KeyCode key)
		{
#if ENABLE_INPUT_SYSTEM
			if (PreferInputSystem())
			{
				var keyboard = Keyboard.current;
				if (keyboard != null && System.Enum.TryParse(key.ToString(), out Key inputKey))
				{
					var keyControl = keyboard[inputKey];
					return keyControl != null && keyControl.wasPressedThisFrame;
				}

				return false;
			}
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
			if (CanUseLegacyInput())
				return UnityEngine.Input.GetKeyDown(key);
#endif
			return false;
		}

		private static bool IsModifierHeld(ShortcutModifiers modifier)
		{
#if ENABLE_INPUT_SYSTEM
			if (PreferInputSystem())
			{
				var keyboard = Keyboard.current;
				if (keyboard != null)
					return IsInputSystemModifierHeld(keyboard, modifier);
			}
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
			if (CanUseLegacyInput())
				return IsLegacyModifierHeld(modifier);
#endif
			return false;
		}

#if ENABLE_INPUT_SYSTEM
		private static bool IsInputSystemModifierHeld(Keyboard keyboard, ShortcutModifiers modifier)
		{
			switch (modifier)
			{
				case ShortcutModifiers.Shift:
					return keyboard.shiftKey.isPressed;
				case ShortcutModifiers.Alt:
					return keyboard.altKey.isPressed;
				case ShortcutModifiers.Action:
					if (Application.platform == RuntimePlatform.OSXEditor)
						return keyboard.leftMetaKey.isPressed || keyboard.rightMetaKey.isPressed;
					return keyboard.ctrlKey.isPressed;
				default:
					return false;
			}
		}
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
		private static bool IsLegacyModifierHeld(ShortcutModifiers modifier)
		{
			switch (modifier)
			{
				case ShortcutModifiers.Shift:
					return UnityEngine.Input.GetKey(KeyCode.LeftShift)
					       || UnityEngine.Input.GetKey(KeyCode.RightShift);
				case ShortcutModifiers.Alt:
					return UnityEngine.Input.GetKey(KeyCode.LeftAlt)
					       || UnityEngine.Input.GetKey(KeyCode.RightAlt);
				case ShortcutModifiers.Action:
					if (Application.platform == RuntimePlatform.OSXEditor)
						return UnityEngine.Input.GetKey(KeyCode.LeftCommand)
						       || UnityEngine.Input.GetKey(KeyCode.RightCommand);
					return UnityEngine.Input.GetKey(KeyCode.LeftControl)
					       || UnityEngine.Input.GetKey(KeyCode.RightControl);
				default:
					return false;
			}
		}
#endif

		private static int ActiveInputHandler
		{
			get
			{
				foreach (var settings in Resources.FindObjectsOfTypeAll<PlayerSettings>())
				{
					var property = new SerializedObject(settings).FindProperty(ActiveInputHandlerProperty);
					if (property != null)
						return property.intValue;
				}

				return 0;
			}
		}

#if ENABLE_INPUT_SYSTEM
		private static bool PreferInputSystem() => ActiveInputHandler != 0;
#endif

#if ENABLE_LEGACY_INPUT_MANAGER
		private static bool CanUseLegacyInput() => ActiveInputHandler != 1;
#endif
	}
}
