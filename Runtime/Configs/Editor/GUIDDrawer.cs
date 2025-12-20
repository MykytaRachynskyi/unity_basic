#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    [CustomPropertyDrawer(typeof(GUID), true)]
    public class GUIDDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var firstHalf = property.FindPropertyRelative("FirstHalf").longValue;
            var secondHalf = property.FindPropertyRelative("SecondHalf").longValue;

            System.Span<byte> bytes = stackalloc byte[16];
            for (int i = 0; i < 8; ++i)
            {
                bytes[i] = (byte)(firstHalf >> (i * 8));
            }

            for (int i = 0; i < 8; ++i)
            {
                bytes[i + 8] = (byte)(secondHalf >> (i * 8));
            }

            var guid = new System.Guid(bytes);
            GUI.Label(position, $"{label}: {guid}");
        }
    }
}
#endif
