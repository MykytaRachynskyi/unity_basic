using UnityEngine;

namespace Basic.Logger
{
	[System.Serializable]
	public class LoggerSettings
	{
		[field: SerializeField]
		public LogLevel DefaultMinLevel { get; private set; } = LogLevel.Verbose;

		[field: SerializeField]
		public bool EnableConsoleSink { get; private set; } = true;

		[field: SerializeField]
		public bool EnableFileSink { get; private set; }
	}
}
