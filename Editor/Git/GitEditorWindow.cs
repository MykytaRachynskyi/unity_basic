using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    public class GitEditorWindow : EditorWindow
    {
        private static readonly Color DefaultBranchColor = new(0.3f, 0.85f, 0.4f);
        private static readonly Color OtherBranchColor = new(0.4f, 0.85f, 1f);
        private static readonly Color DetachedColor = new(1f, 0.4f, 0.4f);
        private static readonly Color ErrorBranchColor = new(0.65f, 0.65f, 0.65f);

        private string _branchLabel = "…";
        private BranchKind _branchKind = BranchKind.Error;
        private string _statusMessage = string.Empty;
        private MessageType _statusType = MessageType.None;

        private bool _pullInProgress;
        private Process _pullProcess;

        private GUIStyle _branchStyle;
        private GUIStyle _pathStyle;

        [MenuItem("Tools/Basic/Git")]
        public static void ShowWindow()
        {
            var window = GetWindow<GitEditorWindow>("Git");
            window.minSize = new Vector2(280f, 140f);
        }

        private void OnEnable()
        {
            RefreshBranch();
        }

        private void OnFocus()
        {
            RefreshBranch();
        }

        private void OnDisable()
        {
            StopPullPolling();
            CleanupPullProcess();
        }

        private void OnGUI()
        {
            EnsureStyles();

            DrawBranchLabel();
            DrawProjectPath();
            EditorGUILayout.Space(6f);
            DrawButtons();
            DrawStatus();
        }

        private void EnsureStyles()
        {
            if (_branchStyle == null)
            {
                _branchStyle = new GUIStyle(EditorStyles.boldLabel)
                {
                    fontSize = 17,
                    wordWrap = true,
                };
            }

            if (_pathStyle == null)
            {
                _pathStyle = new GUIStyle(EditorStyles.miniLabel)
                {
                    wordWrap = true,
                };
            }
        }

        private void DrawBranchLabel()
        {
            var previousColor = GUI.contentColor;
            GUI.contentColor = GetBranchColor(_branchKind);
            EditorGUILayout.LabelField(_branchLabel, _branchStyle);
            GUI.contentColor = previousColor;
        }

        private void DrawProjectPath()
        {
            EditorGUILayout.LabelField(GitCli.RepositoryRoot, _pathStyle);
        }

        private void DrawButtons()
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_pullInProgress))
                {
                    if (GUILayout.Button("Pull", GUILayout.Height(24f)))
                        StartPull();

                    if (GUILayout.Button("Refresh", GUILayout.Height(24f)))
                        RefreshBranch();
                }
            }
        }

        private void DrawStatus()
        {
            if (string.IsNullOrEmpty(_statusMessage) || _statusType == MessageType.None)
                return;

            EditorGUILayout.Space(4f);
            EditorGUILayout.HelpBox(_statusMessage, _statusType);
        }

        private void RefreshBranch()
        {
            var branchInfo = GitCli.GetBranchLabel();
            _branchLabel = branchInfo.Label;
            _branchKind = branchInfo.Kind;
            Repaint();
        }

        private void StartPull()
        {
            if (_pullInProgress)
                return;

            SetStatus("Pulling...", MessageType.Info);

            try
            {
                _pullProcess = GitCli.StartPullFfOnly();
                _pullInProgress = true;
                EditorApplication.update += PollPull;
            }
            catch (Exception exception)
            {
                _pullInProgress = false;
                CleanupPullProcess();
                SetStatus(GetPullStartErrorMessage(exception), MessageType.Error);
            }
        }

        private void PollPull()
        {
            if (_pullProcess == null)
            {
                StopPullPolling();
                _pullInProgress = false;
                return;
            }

            if (!_pullProcess.HasExited)
                return;

            StopPullPolling();

            var exitCode = _pullProcess.ExitCode;
            var stderr = _pullProcess.StandardError.ReadToEnd().Trim();
            var stdout = _pullProcess.StandardOutput.ReadToEnd().Trim();

            CleanupPullProcess();
            _pullInProgress = false;

            if (exitCode == 0)
            {
                SetStatus(
                    string.IsNullOrEmpty(stdout) ? "Pull completed successfully." : stdout,
                    MessageType.Info
                );
                AssetDatabase.Refresh();
                RefreshBranch();
                return;
            }

            var message = !string.IsNullOrEmpty(stderr) ? stderr : stdout;
            if (string.IsNullOrEmpty(message))
                message = $"Pull failed with exit code {exitCode}.";

            SetStatus(message, MessageType.Error);
            RefreshBranch();
        }

        private void StopPullPolling()
        {
            EditorApplication.update -= PollPull;
        }

        private void CleanupPullProcess()
        {
            if (_pullProcess == null)
                return;

            if (_pullProcess.HasExited)
            {
                _pullProcess.Dispose();
                _pullProcess = null;
                return;
            }

            // Let an in-flight pull finish without interrupting git.
            _pullProcess = null;
            _pullInProgress = false;
        }

        private void SetStatus(string message, MessageType type)
        {
            _statusMessage = message;
            _statusType = type;
            Repaint();
        }

        private static Color GetBranchColor(BranchKind kind) =>
            kind switch
            {
                BranchKind.DefaultBranch => DefaultBranchColor,
                BranchKind.OtherBranch => OtherBranchColor,
                BranchKind.Detached => DetachedColor,
                _ => ErrorBranchColor,
            };

        private static string GetPullStartErrorMessage(Exception exception)
        {
            if (exception is Win32Exception || exception is FileNotFoundException)
                return "git executable not found in PATH.";

            return exception.Message;
        }
    }
}
