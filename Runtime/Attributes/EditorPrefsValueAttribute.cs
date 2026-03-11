using System;

namespace Basic
{
	[AttributeUsage(AttributeTargets.Field)]
	public class EditorPrefsValueAttribute : Attribute
	{
		public readonly string Key;
		public EditorPrefsValueAttribute(string key) => Key = key;
	}
}