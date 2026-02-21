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

            var isZeroGuid = firstHalf == 0 && secondHalf == 0;

            if (isZeroGuid)
            {
                const float buttonWidth = 80f;
                const float spacing = 6f;

                var labelRect = new Rect(
                    position.x,
                    position.y,
                    position.width - buttonWidth - spacing,
                    position.height
                );

                var buttonRect = new Rect(
                    position.x + position.width - buttonWidth,
                    position.y,
                    buttonWidth,
                    position.height
                );

                GUI.Label(labelRect, $"{label.text}: <empty>");

                if (GUI.Button(buttonRect, "Generate"))
                {
                    var newGuid = GUID.Generate();

                    property.FindPropertyRelative("FirstHalf").longValue = newGuid.FirstHalf;
                    property.FindPropertyRelative("SecondHalf").longValue = newGuid.SecondHalf;

                    property.serializedObject.ApplyModifiedProperties();
                    EditorUtility.SetDirty(property.serializedObject.targetObject);
                }

                return;
            }

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
