#if UNITY_EDITOR
using Basic.UI;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    [CustomEditor(typeof(ColorPalette))]
    public class ColorPaletteEditor : Editor
    {
        private ReorderableList _entriesList;
        private SerializedProperty _entries;

        private void OnEnable()
        {
            _entries = serializedObject.FindProperty("_entries");
            _entriesList = new ReorderableList(serializedObject, _entries, true, true, true, true)
            {
                drawHeaderCallback = rect => EditorGUI.LabelField(rect, "Colors"),
                drawElementCallback = DrawEntry,
                elementHeightCallback = _ => EditorGUIUtility.singleLineHeight + 4f,
                onAddCallback = OnAddEntry,
            };
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();
            _entriesList.DoLayoutList();
            serializedObject.ApplyModifiedProperties();
        }

        private void DrawEntry(Rect rect, int index, bool isActive, bool isFocused)
        {
            var element = _entries.GetArrayElementAtIndex(index);
            var nameProperty = element.FindPropertyRelative("_name");
            var colorProperty = element.FindPropertyRelative("_color");
            var guidProperty = element.FindPropertyRelative("_configId._guid");

            const float spacing = 4f;
            const float colorWidth = 50f;
            const float guidWidth = 180f;

            var nameRect = new Rect(rect.x, rect.y + 2f, rect.width - colorWidth - guidWidth - spacing * 2f, EditorGUIUtility.singleLineHeight);
            var colorRect = new Rect(nameRect.xMax + spacing, rect.y + 2f, colorWidth, EditorGUIUtility.singleLineHeight);
            var guidRect = new Rect(colorRect.xMax + spacing, rect.y + 2f, guidWidth, EditorGUIUtility.singleLineHeight);

            EditorGUI.PropertyField(nameRect, nameProperty, GUIContent.none);
            EditorGUI.PropertyField(colorRect, colorProperty, GUIContent.none);
            EditorGUI.PropertyField(guidRect, guidProperty, GUIContent.none);
        }

        private void OnAddEntry(ReorderableList list)
        {
            var index = list.serializedProperty.arraySize;
            list.serializedProperty.arraySize++;
            var element = list.serializedProperty.GetArrayElementAtIndex(index);

            element.FindPropertyRelative("_name").stringValue = "New Color";
            element.FindPropertyRelative("_color").colorValue = Color.white;

            var guid = GUID.Generate();
            var guidProperty = element.FindPropertyRelative("_configId._guid");
            guidProperty.FindPropertyRelative("FirstHalf").longValue = guid.FirstHalf;
            guidProperty.FindPropertyRelative("SecondHalf").longValue = guid.SecondHalf;
        }
    }
}
#endif
