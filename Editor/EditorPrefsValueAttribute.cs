using UnityEngine;

namespace Basic
{
	public class EditorPrefsValueAttribute : PropertyAttribute
	{
		public readonly string Key;
		public EditorPrefsValueAttribute(string key) => Key = key;
	}
}