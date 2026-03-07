using System;
using System.IO;
using UnityEngine;

namespace Basic.Logger
{
	public class FileSink : ILogSink, IDisposable
	{
		private readonly StreamWriter _writer;
		private bool _disposed;

		public FileSink() : this(DefaultLogPath()) { }

		public FileSink(string filePath)
		{
			var directory = Path.GetDirectoryName(filePath);
			if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
				Directory.CreateDirectory(directory);

			_writer = new StreamWriter(filePath, append: true) { AutoFlush = true };
		}

		public void Emit(in LogEntry entry)
		{
			if (_disposed) return;

			var levelTag = entry.Level.ToString().ToUpperInvariant();
			var line = $"[{entry.Timestamp}] [Frame {entry.Frame}] [{levelTag}] [{entry.Module}] {entry.Message}";

			lock (_writer)
			{
				_writer.WriteLine(line);
			}
		}

		public void Dispose()
		{
			if (_disposed) return;
			_disposed = true;
			_writer?.Flush();
			_writer?.Dispose();
		}

		private static string DefaultLogPath()
		{
			var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
			return Path.Combine(Application.persistentDataPath, "Logs", $"log_{timestamp}.txt");
		}
	}
}
