using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Basic.UnityEditorTools
{
	public class EditorTools
	{
		public static bool TryLoadAssetFromAssetDatabase<T>(out T obj)
			where T : Object
		{
			obj = null;

#if UNITY_EDITOR
			var assetGUIDs = AssetDatabase.FindAssets($"t: {typeof(T).Name}");
			if (assetGUIDs == null || assetGUIDs.Length == 0)
			{
				return false;
			}

			foreach (var guid in assetGUIDs)
			{
				var path = AssetDatabase.GUIDToAssetPath(guid);
				obj = AssetDatabase.LoadAssetAtPath(path, typeof(T)) as T;

				if (obj != null)
				{
					return true;
				}
			}
#endif

			return false;
		}
	}
}
