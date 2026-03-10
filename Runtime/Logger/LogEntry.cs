using System;

namespace Basic.Logger
{
	public readonly struct LogEntry
	{
		public readonly LogLevel Level;
		public readonly string Message;
		public readonly string Module;
		public readonly string Timestamp;
		public readonly int Frame;
		public readonly Exception Exception;
		public readonly bool IsAssertion;

		public LogEntry(LogLevel level, string message, string module, string timestamp, int frame)
			: this(level, message, module, timestamp, frame, null, false) { }

		public LogEntry(LogLevel level, string message, string module, string timestamp, int frame,
			Exception exception, bool isAssertion)
		{
			Level = level;
			Message = message;
			Module = module;
			Timestamp = timestamp;
			Frame = frame;
			Exception = exception;
			IsAssertion = isAssertion;
		}
	}
}
