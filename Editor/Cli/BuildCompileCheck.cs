using System.IO;
using Basic;
using UnityEditor;
using UnityEditor.Build.Player;

namespace Basic.UnityEditorTools
{
    /// <summary>
    /// Verifies that project scripts compile for a player build target.
    /// </summary>
    public static class BuildCompileCheck
    {
        /// <summary>
        /// <para>Run via batchmode:</para>
        /// <code>
        /// Unity -batchmode -nographics -quit
        ///   -projectPath &lt;path&gt;
        ///   -buildTarget StandaloneWindows64
        ///   -executeMethod Basic.UnityEditorTools.BuildCompileCheck.Run
        ///   -logFile -
        /// </code>
        /// <para>Optional args: <c>-basicOutputPath</c> overrides the temp player script output folder.</para>
        /// </summary>
        public static void Run()
        {
            var target = CliRunner.ResolveBuildTarget();
            CliRunner.EnsureBuildTarget(target);

            var outputDirectory = CliRunner.GetPlayerScriptOutputDirectory();
            if (Directory.Exists(outputDirectory))
                Directory.Delete(outputDirectory, recursive: true);
            Directory.CreateDirectory(outputDirectory);

            var settings = new ScriptCompilationSettings
            {
                group = BuildPipeline.GetBuildTargetGroup(target),
                target = target,
                options = ScriptCompilationOptions.DevelopmentBuild,
            };

            Log.CliInfo($"[Basic/Cli] Running build compile check for {target} -> {outputDirectory}");
            ScriptCompilationResult result = PlayerBuildInterface.CompilePlayerScripts(settings, outputDirectory);

            if (EditorUtility.scriptCompilationFailed)
            {
                CliRunner.ExitFailure("Build compile check FAILED — player script compilation reported errors.");
            }

            if (result.assemblies == null || result.assemblies.Count == 0)
            {
                CliRunner.ExitFailure("Build compile check FAILED — no player assemblies were produced.");
            }

            CliRunner.TryDeleteOutput(outputDirectory);
            CliRunner.ExitSuccess("Build compile check passed.");
        }
    }
}
