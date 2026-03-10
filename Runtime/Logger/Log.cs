using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Basic.Logger;
using UnityEngine;

namespace Basic
{
    public static class Log
    {
        private static readonly List<ILogSink> Sinks = new();
        private static readonly object SinkLock = new();
        private static LogLevel _minLevel = LogLevel.Verbose;
        private static bool _initialized;

        // ───────────────────────── Configuration ─────────────────────────

        public static void AddSink(ILogSink sink)
        {
            if (sink == null)
                return;
            lock (SinkLock)
            {
                Sinks.Add(sink);
            }
        }

        public static void RemoveSink(ILogSink sink)
        {
            if (sink == null)
                return;
            lock (SinkLock)
            {
                Sinks.Remove(sink);
            }
        }

        public static void SetMinLevel(LogLevel level) => _minLevel = level;

        public static bool IsEnabled(LogLevel level) => level >= _minLevel;

        // ───────────────────────── Verbose ─────────────────────────

        [HideInCallstack]
        [Conditional("ENABLE_LOGGING")]
        public static void Verbose(string message, [CallerFilePath] string filePath = "")
        {
            Emit(LogLevel.Verbose, message, ModuleNameResolver.Resolve(filePath));
        }

        // ───────────────────────── Debug ─────────────────────────

        [HideInCallstack]
        [Conditional("ENABLE_LOGGING")]
        public static void Debug(string message, [CallerFilePath] string filePath = "")
        {
            Emit(LogLevel.Debug, message, ModuleNameResolver.Resolve(filePath));
        }

        // ───────────────────────── Info ─────────────────────────

        [HideInCallstack]
        [Conditional("ENABLE_LOGGING")]
        public static void Info(string message, [CallerFilePath] string filePath = "")
        {
            Emit(LogLevel.Info, message, ModuleNameResolver.Resolve(filePath));
        }

        // ───────────────────────── Warning ─────────────────────────

        [HideInCallstack]
        [Conditional("ENABLE_LOGGING")]
        public static void Warning(string message, [CallerFilePath] string filePath = "")
        {
            Emit(LogLevel.Warning, message, ModuleNameResolver.Resolve(filePath));
        }

        // ───────────────────────── Error (always compiled) ─────────────────────────

        [HideInCallstack]
        public static void Error(string message, [CallerFilePath] string filePath = "")
        {
            Emit(LogLevel.Error, message, ModuleNameResolver.Resolve(filePath));
        }

        // ───────────────────────── Fatal (always compiled) ─────────────────────────

        [HideInCallstack]
        public static void Fatal(string message, [CallerFilePath] string filePath = "")
        {
            Emit(LogLevel.Fatal, message, ModuleNameResolver.Resolve(filePath));
        }

        // ───────────────────────── Exception (always compiled) ─────────────────────────

        [HideInCallstack]
        public static void Exception(Exception exception)
        {
            if (exception == null)
                return;

            var module = ModuleNameResolver.ResolveFromException(exception);

            EnsureInitialized();

            var entry = new LogEntry(
                LogLevel.Error,
                exception.Message,
                module,
                DateTime.Now.ToString("HH:mm:ss.fff"),
                Time.frameCount,
                exception,
                false
            );

            lock (SinkLock)
            {
                for (var i = 0; i < Sinks.Count; i++)
                    Sinks[i].Emit(in entry);
            }
        }

        // ───────────────────────── Assert ─────────────────────────

        [HideInCallstack]
        [Conditional("ENABLE_LOGGING")]
        public static void Assert(bool condition, string message = "", [CallerFilePath] string filePath = "")
        {
            if (condition)
                return;

            var module = ModuleNameResolver.Resolve(filePath);
            var assertMessage = string.IsNullOrEmpty(message)
                ? "Assertion Failed"
                : $"Assertion Failed: {message}";

            EnsureInitialized();

            var entry = new LogEntry(
                LogLevel.Error,
                assertMessage,
                module,
                DateTime.Now.ToString("HH:mm:ss.fff"),
                Time.frameCount,
                null,
                true
            );

            lock (SinkLock)
            {
                for (var i = 0; i < Sinks.Count; i++)
                    Sinks[i].Emit(in entry);
            }
        }

        // ───────────────────────── Internal ─────────────────────────

        [HideInCallstack]
        private static void Emit(LogLevel level, string message, string module)
        {
            if (level < _minLevel)
                return;

            EnsureInitialized();

            var entry = new LogEntry(
                level,
                message,
                module,
                DateTime.Now.ToString("HH:mm:ss.fff"),
                Time.frameCount
            );

            lock (SinkLock)
            {
                for (var i = 0; i < Sinks.Count; i++)
                    Sinks[i].Emit(in entry);
            }
        }

        private static void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            var settings = LoggerSettingsContainer.Settings;
            if (settings != null)
            {
                _minLevel = settings.DefaultMinLevel;

                if (settings.EnableConsoleSink)
                    Sinks.Add(new UnityConsoleSink());

                if (settings.EnableFileSink)
                    Sinks.Add(new FileSink());
            }

            if (Sinks.Count == 0)
                Sinks.Add(new UnityConsoleSink());
        }
    }
}
