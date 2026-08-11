using System;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    public enum BranchKind
    {
        DefaultBranch,
        OtherBranch,
        Detached,
        NotRepository,
        GitNotFound,
        Error,
    }

    public readonly struct GitResult
    {
        public bool Success { get; }
        public int ExitCode { get; }
        public string Stdout { get; }
        public string Stderr { get; }

        public GitResult(bool success, int exitCode, string stdout, string stderr)
        {
            Success = success;
            ExitCode = exitCode;
            Stdout = stdout ?? string.Empty;
            Stderr = stderr ?? string.Empty;
        }
    }

    public readonly struct BranchInfo
    {
        public string Label { get; }
        public BranchKind Kind { get; }

        public BranchInfo(string label, BranchKind kind)
        {
            Label = label;
            Kind = kind;
        }
    }

    public static class GitCli
    {
        public static string RepositoryRoot { get; } =
            Path.GetFullPath(Path.GetDirectoryName(Application.dataPath) ?? string.Empty);

        public static GitResult Run(string arguments)
        {
            try
            {
                using var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "git",
                        Arguments = arguments,
                        WorkingDirectory = RepositoryRoot,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        CreateNoWindow = true,
                    },
                };

                process.Start();
                var stdout = process.StandardOutput.ReadToEnd();
                var stderr = process.StandardError.ReadToEnd();
                process.WaitForExit();

                return new GitResult(
                    process.ExitCode == 0,
                    process.ExitCode,
                    stdout.Trim(),
                    stderr.Trim()
                );
            }
            catch (Exception exception) when (IsGitNotFound(exception))
            {
                return new GitResult(false, -1, string.Empty, "git executable not found in PATH.");
            }
        }

        public static BranchInfo GetBranchLabel()
        {
            var branchResult = Run("rev-parse --abbrev-ref HEAD");
            if (!branchResult.Success)
            {
                if (branchResult.Stderr.Contains("not a git repository", StringComparison.OrdinalIgnoreCase))
                    return new BranchInfo("Not a git repository", BranchKind.NotRepository);

                if (branchResult.ExitCode == -1)
                    return new BranchInfo("git not found", BranchKind.GitNotFound);

                return new BranchInfo(
                    string.IsNullOrEmpty(branchResult.Stderr) ? "Unknown git error" : branchResult.Stderr,
                    BranchKind.Error
                );
            }

            if (branchResult.Stdout.Equals("HEAD", StringComparison.Ordinal))
            {
                var hashResult = Run("rev-parse --short HEAD");
                if (!hashResult.Success)
                    return new BranchInfo("HEAD (detached)", BranchKind.Detached);

                return new BranchInfo($"{hashResult.Stdout} (detached)", BranchKind.Detached);
            }

            if (IsDefaultBranch(branchResult.Stdout))
                return new BranchInfo(branchResult.Stdout, BranchKind.DefaultBranch);

            return new BranchInfo(branchResult.Stdout, BranchKind.OtherBranch);
        }

        public static Process StartPullFfOnly()
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = "pull --ff-only",
                    WorkingDirectory = RepositoryRoot,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true,
                },
                EnableRaisingEvents = true,
            };

            process.Start();
            return process;
        }

        private static bool IsDefaultBranch(string branchName) =>
            branchName.Equals("main", StringComparison.OrdinalIgnoreCase)
            || branchName.Equals("master", StringComparison.OrdinalIgnoreCase);

        private static bool IsGitNotFound(Exception exception) =>
            exception is Win32Exception { NativeErrorCode: 2 }
            || exception is FileNotFoundException;
    }
}
