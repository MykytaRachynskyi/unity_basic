using UnityEditor;
using UnityEditor.Compilation;
using UnityEditor.ShortcutManagement;
using UnityEngine;

namespace Basic.UnityEditorTools
{
	public static class RequestScriptCompilationShortcut
	{
		public const string ShortcutId = "Basic/Request Script Compilation";

		[Shortcut(ShortcutId, KeyCode.R, ShortcutModifiers.Action | ShortcutModifiers.Shift | ShortcutModifiers.Alt)]
		private static void RequestRecompile()
		{
			// CleanBuildCache forces a full rebuild so compiler warnings reappear in the Console.
			CompilationPipeline.RequestScriptCompilation(
				RequestScriptCompilationOptions.CleanBuildCache
			);
		}
	}
}

