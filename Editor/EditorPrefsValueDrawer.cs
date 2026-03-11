using System.Collections.Generic;
using System.Reflection;
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

            var owner = ResolveOwner(property);
            var value = JsonConvert.DeserializeObject(
                EditorPrefs.GetString(key),
                fieldInfo.FieldType
            );
            fieldInfo.SetValue(owner, value);
            property.serializedObject.Update();
        }

        void Save(SerializedProperty property, string key)
        {
            var owner = ResolveOwner(property);
            EditorPrefs.SetString(key, JsonConvert.SerializeObject(fieldInfo.GetValue(owner)));
        }

        // Walks the propertyPath to find the object that directly owns this field,
        // which may be a nested object rather than the root targetObject.
        object ResolveOwner(SerializedProperty property)
        {
            object current = property.serializedObject.targetObject;

            // Strip the final field name — we want the owner, not the value itself
            var path = property.propertyPath.Replace(".Array.data[", "[");
            var parts = path.Split('.');

            for (int i = 0; i < parts.Length - 1; i++)
            {
                var part = parts[i];
                if (part.EndsWith("]"))
                {
                    int bracket = part.IndexOf('[');
                    int index = int.Parse(part.Substring(bracket + 1, part.Length - bracket - 2));
                    var list = (System.Collections.IList)
                        GetField(current, part.Substring(0, bracket)).GetValue(current);
                    current = list[index];
                }
                else
                {
                    current = GetField(current, part).GetValue(current);
                }
            }

            return current;
        }

        static FieldInfo GetField(object obj, string name)
        {
            var type = obj.GetType();
            while (type != null)
            {
                var f = type.GetField(
                    name,
                    BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
                );
                if (f != null)
                    return f;
                type = type.BaseType;
            }
            throw new System.Exception($"Field '{name}' not found on {obj.GetType()}");
        }
    }
}
