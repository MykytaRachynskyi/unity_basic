using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Basic.UnityEditorTools
{
	public static class EventSystemHoverSettings
	{
		public const string PreferencesPath = "Preferences/Basic/Event System Hover";

		private const string SelectKeyPref = "Basic.EventSystemHover.SelectKey";
		private const string SelectModifiersPref = "Basic.EventSystemHover.SelectModifiers";

		private static KeyCode _selectKey = KeyCode.H;
		private static ShortcutModifiers _selectModifiers = ShortcutModifiers.Shift;
		private static bool _loaded;

		public static KeyCode SelectKey
		{
			get
			{
				EnsureLoaded();
				return _selectKey;
			}
			set
			{
				EnsureLoaded();
				_selectKey = value;
				EditorPrefs.SetInt(SelectKeyPref, (int)value);
			}
		}

		public static ShortcutModifiers SelectModifiers
		{
			get
			{
				EnsureLoaded();
				return _selectModifiers;
			}
			set
			{
				EnsureLoaded();
				_selectModifiers = value;
				EditorPrefs.SetInt(SelectModifiersPref, (int)value);
			}
		}

		public static string HotkeyDisplayString
		{
			get
			{
				var parts = new System.Collections.Generic.List<string>(3);
				var modifiers = SelectModifiers;
				if ((modifiers & ShortcutModifiers.Shift) != 0)
					parts.Add("Shift");
				if ((modifiers & ShortcutModifiers.Alt) != 0)
					parts.Add("Alt");
				if ((modifiers & ShortcutModifiers.Control) != 0)
					parts.Add("Ctrl");
				else if ((modifiers & ShortcutModifiers.Action) != 0)
					parts.Add(Application.platform == RuntimePlatform.OSXEditor ? "Cmd" : "Ctrl");
				parts.Add(SelectKey.ToString());
				return string.Join("+", parts);
			}
		}

		public static bool IsHotkeyPressedThisFrame() =>
			EventSystemHoverInput.IsHotkeyPressedThisFrame(SelectKey, SelectModifiers);

		private static void EnsureLoaded()
		{
			if (_loaded)
				return;

			_selectKey = (KeyCode)EditorPrefs.GetInt(SelectKeyPref, (int)KeyCode.H);
			_selectModifiers = (ShortcutModifiers)EditorPrefs.GetInt(
				SelectModifiersPref,
				(int)ShortcutModifiers.Shift
			);
			_loaded = true;
		}

	}
}
