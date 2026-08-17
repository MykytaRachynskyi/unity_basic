using UnityEditor;
using UnityEditor.ShortcutManagement;
using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace Basic.UnityEditorTools
{
	[InitializeOnLoad]
	public static class EventSystemHoverHotkey
	{
		public const string ShortcutId = "Basic/Event System Hover/Select Hovered";

		static bool _loggedReadOnlyProfileSkip;

		static EventSystemHoverHotkey()
		{
			EditorApplication.update += OnLegacyEditorUpdate;
			EditorApplication.delayCall += ApplyShortcutBinding;
#if ENABLE_INPUT_SYSTEM
			InputSystem.onAfterUpdate += OnInputAfterUpdate;
#endif
		}

		[Shortcut(ShortcutId, KeyCode.H, ShortcutModifiers.Shift)]
		private static void OnShortcut()
		{
			if (!EditorApplication.isPlaying)
				return;

			EventSystemHoverToolbar.TrySelectHovered();
		}

		public static void ApplyShortcutBinding()
		{
			var shortcutManager = ShortcutManager.instance;
			if (shortcutManager.IsProfileReadOnly(shortcutManager.activeProfileId))
			{
				if (!_loggedReadOnlyProfileSkip)
				{
					_loggedReadOnlyProfileSkip = true;
					Log.Warning(
						"Skipping shortcut rebind: active profile is read-only. "
						+ "Create a custom profile in Edit → Shortcuts to customize the binding."
					);
				}

				return;
			}

			shortcutManager.RebindShortcut(
				ShortcutId,
				new ShortcutBinding(
					new KeyCombination(
						EventSystemHoverSettings.SelectKey,
						EventSystemHoverSettings.SelectModifiers
					)
				)
			);
		}

#if ENABLE_INPUT_SYSTEM
		private static void OnInputAfterUpdate()
		{
			if (!EditorApplication.isPlaying)
				return;

			if (EventSystemHoverInput.ShouldPollInputSystemHotkey)
				EventSystemHoverToolbar.UpdateHoverState();

			if (EventSystemHoverSettings.IsHotkeyPressedThisFrame())
				EventSystemHoverToolbar.TrySelectHovered();
		}
#endif

		private static void OnLegacyEditorUpdate()
		{
			if (!EditorApplication.isPlaying || !EventSystemHoverInput.ShouldPollLegacyHotkey)
				return;

			if (EventSystemHoverSettings.IsHotkeyPressedThisFrame())
				EventSystemHoverToolbar.TrySelectHovered();
		}
	}
}
