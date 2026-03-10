using System;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Basic.Logger
{
    public class UnityConsoleSink : ILogSink
    {
        [HideInCallstack]
        public void Emit(in LogEntry entry)
        {
            var formatted = Format(in entry);

            if (entry.Exception != null)
            {
                EmitError(formatted);
                EmitException(entry.Exception);
                return;
            }

            if (entry.IsAssertion)
            {
                EmitAssertion(formatted);
                return;
            }

            switch (entry.Level)
            {
                case LogLevel.Verbose:
                case LogLevel.Debug:
                case LogLevel.Info:
                    EmitLog(formatted);
                    break;

                case LogLevel.Warning:
                    EmitWarning(formatted);
                    break;

                case LogLevel.Error:
                case LogLevel.Fatal:
                    EmitError(formatted);
                    break;
            }
        }

        [HideInCallstack]
        private static void EmitLog(string message) => Debug.Log(message);

        [HideInCallstack]
        private static void EmitWarning(string message) => Debug.LogWarning(message);

        [HideInCallstack]
        private static void EmitError(string message) => Debug.LogError(message);

        [HideInCallstack]
        private static void EmitException(Exception exception) => Debug.LogException(exception);

        [HideInCallstack]
        private static void EmitAssertion(string message) => Debug.LogAssertion(message);

        private static string Format(in LogEntry entry)
        {
            var color = GetColor(entry.Level);
            var levelTag = entry.Level.ToString().ToUpperInvariant();

            return $"<color={color}>[{entry.Timestamp}] [Frame {entry.Frame}] [{levelTag}]</color> [<b>{entry.Module}</b>] {entry.Message}";
        }

        private static string GetColor(LogLevel level)
        {
            return level switch
            {
                LogLevel.Verbose => "#888888",
                LogLevel.Debug => "#AAAAAA",
                LogLevel.Info => "#CCCCCC",
                LogLevel.Warning => "#FFCC00",
                LogLevel.Error => "#FF4444",
                LogLevel.Fatal => "#FF0000",
                _ => "#CCCCCC",
            };
        }
    }
}
