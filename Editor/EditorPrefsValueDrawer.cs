using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Basic
{
    [CustomPropertyDrawer(typeof(EditorPrefsValueAttribute))]
    public class EditorPrefsValueDrawer : PropertyDrawer
    {
        static readonly HashSet<string> _loaded = new();

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label) =>
            EditorGUI.GetPropertyHeight(property, label, true);

        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            var key = ((EditorPrefsValueAttribute)attribute).Key;
            string uid =
                $"{property.serializedObject.targetObject.GetInstanceID()}.{property.propertyPath}";

            if (_loaded.Add(uid))
                Load(property, key);

            EditorGUI.BeginChangeCheck();
            EditorGUI.PropertyField(position, property, label, true);

            if (EditorGUI.EndChangeCheck())
            {
                property.serializedObject.ApplyModifiedPropertiesWithoutUndo();
                Save(property, key);
            }
        }

        void Load(SerializedProperty property, string key)
        {
            if (!EditorPrefs.HasKey(key))
                return;

            var target = property.serializedObject.targetObject;
            var value = JsonConvert.DeserializeObject(
                EditorPrefs.GetString(key),
                fieldInfo.FieldType
            );
            fieldInfo.SetValue(target, value);
            property.serializedObject.Update();
        }

        void Save(SerializedProperty property, string key)
        {
            var value = fieldInfo.GetValue(property.serializedObject.targetObject);
            EditorPrefs.SetString(key, JsonConvert.SerializeObject(value));
        }
    }
}
