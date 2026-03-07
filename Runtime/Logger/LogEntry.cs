namespace Basic.Logger
{
	public readonly struct LogEntry
	{
		public readonly LogLevel Level;
		public readonly string Message;
		public readonly string Module;
		public readonly string Timestamp;
		public readonly int Frame;

		public LogEntry(LogLevel level, string message, string module, string timestamp, int frame)
		{
			Level = level;
			Message = message;
			Module = module;
			Timestamp = timestamp;
			Frame = frame;
		}
	}
}
