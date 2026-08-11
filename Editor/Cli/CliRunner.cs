using System;
using System.Collections.Generic;
using System.IO;
using Basic;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    /// <summary>
    /// Shared helpers for batchmode CLI commands invoked via <c>-executeMethod</c>.
    /// </summary>
    public static class CliRunner
    {
        const string LogPrefix = "[Basic/Cli]";

        /// <summary>
        /// Logs a success message and exits Unity with code 0.
        /// </summary>
        public static void ExitSuccess(string message)
        {
            Log.CliInfo($"{LogPrefix} {message}");
            EditorApplication.Exit(0);
        }

        /// <summary>
        /// Logs a failure message and exits Unity with the given code (default 1).
        /// </summary>
        public static void ExitFailure(string message, int exitCode = 1)
        {
            Log.Error($"{LogPrefix} {message}");
            EditorApplication.Exit(exitCode);
        }

        /// <summary>
        /// Reads a custom command-line argument. Supports <c>-name value</c> and <c>-name=value</c>.
        /// </summary>
        public static bool TryGetCommandLineArg(string name, out string value)
        {
            value = null;
            if (string.IsNullOrEmpty(name))
                return false;

            var args = Environment.GetCommandLineArgs();
            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                if (arg.Equals(name, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 < args.Length && !args[i + 1].StartsWith("-", StringComparison.Ordinal))
                    {
                        value = args[i + 1];
                        return true;
                    }

                    return false;
                }

                if (arg.StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                {
                    value = arg[(name.Length + 1)..];
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Resolves the build target from <c>-buildTarget</c> when provided, otherwise
        /// <see cref="EditorUserBuildSettings.activeBuildTarget"/>, falling back to
        /// <see cref="BuildTarget.StandaloneWindows64"/>.
        /// </summary>
        public static BuildTarget ResolveBuildTarget()
        {
            if (TryGetCommandLineArg("-buildTarget", out var targetName)
                && Enum.TryParse(targetName, ignoreCase: true, out BuildTarget parsed))
                return parsed;

            var active = EditorUserBuildSettings.activeBuildTarget;
            if (active != BuildTarget.NoTarget)
                return active;

            Log.CliInfo($"{LogPrefix} No build target resolved; defaulting to StandaloneWindows64.");
            return BuildTarget.StandaloneWindows64;
        }

        /// <summary>
        /// Returns enabled scene paths from build settings. Logs a warning when none are configured.
        /// </summary>
        public static string[] GetBuildScenes()
        {
            var scenes = EditorBuildSettings.scenes;
            if (scenes == null || scenes.Length == 0)
            {
                Log.CliInfo($"{LogPrefix} No scenes in Editor Build Settings.");
                return Array.Empty<string>();
            }

            var enabled = new List<string>();
            foreach (var scene in scenes)
            {
                if (scene.enabled && !string.IsNullOrEmpty(scene.path))
                    enabled.Add(scene.path);
            }

            if (enabled.Count == 0)
                Log.CliInfo($"{LogPrefix} No enabled scenes in Editor Build Settings.");

            return enabled.ToArray();
        }

        /// <summary>
        /// Returns a throwaway folder for compiled player scripts under <c>Temp/Basic/ScriptCheck/Assemblies</c>.
        /// Override with <c>-basicOutputPath</c>.
        /// </summary>
        public static string GetPlayerScriptOutputDirectory()
        {
            if (TryGetCommandLineArg("-basicOutputPath", out var customPath) && !string.IsNullOrWhiteSpace(customPath))
                return customPath;

            return Path.Combine("Temp", "Basic", "ScriptCheck", "Assemblies");
        }

        /// <summary>
        /// Returns a throwaway player output path under <c>Temp/Basic/ScriptCheck/</c> for the given target.
        /// Override with <c>-basicOutputPath</c>.
        /// </summary>
        public static string GetScriptCheckOutputPath(BuildTarget target)
        {
            if (TryGetCommandLineArg("-basicOutputPath", out var customPath) && !string.IsNullOrWhiteSpace(customPath))
                return customPath;

            var fileName = GetScriptCheckFileName(target);
            var directory = Path.Combine("Temp", "Basic", "ScriptCheck");
            return Path.Combine(directory, fileName);
        }

        /// <summary>
        /// Switches the active build target when it differs from <paramref name="target"/>.
        /// </summary>
        public static void EnsureBuildTarget(BuildTarget target)
        {
            if (EditorUserBuildSettings.activeBuildTarget == target)
                return;

            var group = BuildPipeline.GetBuildTargetGroup(target);
            if (!EditorUserBuildSettings.SwitchActiveBuildTarget(group, target))
                ExitFailure($"Failed to switch build target to {target}.");
        }

        /// <summary>
        /// Logs build-step errors from a <see cref="BuildReport"/> for actionable agent output.
        /// </summary>
        public static void LogBuildFailures(BuildReport report)
        {
            if (report == null)
                return;

            foreach (var step in report.steps)
            {
                if (step.messages == null)
                    continue;

                foreach (var message in step.messages)
                {
                    if (message.type == LogType.Error || message.type == LogType.Exception)
                        Log.Error($"{LogPrefix} [{step.name}] {message.content}");
                }
            }
        }

        /// <summary>
        /// Best-effort deletion of a temp build output path and its parent folder when empty.
        /// </summary>
        public static void TryDeleteOutput(string outputPath)
        {
            if (string.IsNullOrEmpty(outputPath))
                return;

            try
            {
                if (File.Exists(outputPath))
                    File.Delete(outputPath);
                else if (Directory.Exists(outputPath))
                    Directory.Delete(outputPath, recursive: true);

                var parent = Path.GetDirectoryName(outputPath);
                if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
                {
                    if (Directory.GetFileSystemEntries(parent).Length == 0)
                        Directory.Delete(parent);
                }
            }
            catch (Exception ex)
            {
                Log.Warning($"{LogPrefix} Could not delete temp output at '{outputPath}': {ex.Message}");
            }
        }

        static string GetScriptCheckFileName(BuildTarget target)
        {
            return target switch
            {
                BuildTarget.StandaloneWindows or BuildTarget.StandaloneWindows64 => "ScriptCheck.exe",
                BuildTarget.StandaloneOSX => "ScriptCheck.app",
                BuildTarget.Android => "ScriptCheck.apk",
                BuildTarget.iOS => "ScriptCheck",
                BuildTarget.WebGL => "ScriptCheck",
                BuildTarget.StandaloneLinux64 => "ScriptCheck",
                _ => "ScriptCheck",
            };
        }
    }
}
