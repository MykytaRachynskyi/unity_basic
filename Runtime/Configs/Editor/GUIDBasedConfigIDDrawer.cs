#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    [CustomPropertyDrawer(typeof(GUIDBasedConfigID), true)]
    public class GUIDBasedConfigIDDrawer : PropertyDrawer
    {
        private string[] names;

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var configID = (GUIDBasedConfigID)property.boxedValue;

            var index = configID.GUIDToIndex(configID.GUID);
            var allNames = new List<string>();
            configID.GetNames(allNames);

            if (names == null || names.Length < allNames.Count)
            {
                names = new string[allNames.Count];
            }

            for (int i = 0; i < names.Length; ++i)
            {
                names[i] = allNames[i];
            }

            var newIndex = EditorGUI.Popup(position, label.text, index, names);

            if (newIndex != index)
            {
                var guidToSet = configID.IndexToGUID(newIndex);

                var guidProperty = property.FindPropertyRelative("_guid");
                guidProperty.FindPropertyRelative("FirstHalf").longValue = guidToSet.FirstHalf;
                guidProperty.FindPropertyRelative("SecondHalf").longValue = guidToSet.SecondHalf;

                property.serializedObject.ApplyModifiedProperties();
                EditorUtility.SetDirty(property.serializedObject.targetObject);
            }
        }
    }
}
#endif
