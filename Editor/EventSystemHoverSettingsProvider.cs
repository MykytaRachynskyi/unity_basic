using System.Collections.Generic;
using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEditor.Toolbars;
using UnityEngine;

namespace Basic.UnityEditorTools
{
	public static class EventSystemHoverSettingsProvider
	{
		[SettingsProvider]
		public static SettingsProvider CreateProvider()
		{
			return new SettingsProvider(EventSystemHoverSettings.PreferencesPath, SettingsScope.User)
			{
				label = "Event System Hover",
				keywords = new HashSet<string>(new[] { "EventSystem", "Hover", "Raycast", "Hotkey", "Basic" }),
				guiHandler = DrawSettings
			};
		}

		private static void DrawSettings(string searchContext)
		{
			EditorGUILayout.LabelField("Event System Hover", EditorStyles.boldLabel);
			EditorGUILayout.Space(4);

			EditorGUI.BeginChangeCheck();

			var key = (KeyCode)EditorGUILayout.EnumPopup("Select Hovered Key", EventSystemHoverSettings.SelectKey);
			var modifiers = (ShortcutModifiers)EditorGUILayout.EnumFlagsField(
				"Select Hovered Modifiers",
				EventSystemHoverSettings.SelectModifiers
			);

			if (EditorGUI.EndChangeCheck())
			{
				EventSystemHoverSettings.SelectKey = key;
				EventSystemHoverSettings.SelectModifiers = modifiers;
				EventSystemHoverHotkey.ApplyShortcutBinding();
				MainToolbar.Refresh(EventSystemHoverToolbar.ToolbarPath);
			}

			EditorGUILayout.Space(8);
			EditorGUILayout.HelpBox(
				$"During Play Mode, press {EventSystemHoverSettings.HotkeyDisplayString} to select "
				+ "the GameObject currently under the mouse according to the EventSystem.",
				MessageType.Info
			);
		}
	}
}
