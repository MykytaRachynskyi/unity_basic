using System;
using System.Collections.Generic;
using System.Reflection;
using NaughtyAttributes.Editor;
using Newtonsoft.Json;
using UnityEditor;
using UnityEngine;

namespace Basic
{
    [InitializeOnLoad]
    public static class EditorPrefsValueDrawer
    {
        static readonly Dictionary<string, object> _cache = new();
        static readonly Dictionary<Type, FieldInfo[]> _fieldsPerType = new();

        static EditorPrefsValueDrawer()
        {
            NaughtyInspector.RegisterAdditionalDrawer(DrawTarget);
        }

        static FieldInfo[] GetFields(Type type)
        {
            if (!_fieldsPerType.TryGetValue(type, out var fields))
            {
                var list = new List<FieldInfo>();
                var t = type;
                while (t != null)
                {
                    foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                        if (f.GetCustomAttribute<EditorPrefsValueAttribute>() != null)
                            list.Add(f);
                    t = t.BaseType;
                }
                fields = list.ToArray();
                _fieldsPerType[type] = fields;
            }
            return fields;
        }

        static void DrawTarget(UnityEngine.Object target)
        {
            var fields = GetFields(target.GetType());
            foreach (var field in fields)
            {
                var attr = field.GetCustomAttribute<EditorPrefsValueAttribute>();
                DrawField(target, field, attr.Key);
            }
        }

        static void DrawField(UnityEngine.Object target, FieldInfo field, string key)
        {
            if (!_cache.ContainsKey(key))
            {
                if (EditorPrefs.HasKey(key))
                {
                    try
                    {
                        _cache[key] = JsonConvert.DeserializeObject(
                            EditorPrefs.GetString(key), field.FieldType);
                    }
                    catch
                    {
                        _cache[key] = field.GetValue(target);
                    }
                }
                else
                {
                    _cache[key] = field.GetValue(target);
                }
            }

            field.SetValue(target, _cache[key]);

            string label = ObjectNames.NicifyVariableName(field.Name);
            object current = _cache[key];
            object newValue = DrawEditableField(current, field.FieldType, label);

            if (!Equals(current, newValue))
            {
                _cache[key] = newValue;
                field.SetValue(target, newValue);
                EditorPrefs.SetString(key, JsonConvert.SerializeObject(newValue));
            }
        }

        static object DrawEditableField(object value, Type type, string label)
        {
            if (type == typeof(bool))
                return EditorGUILayout.Toggle(label, (bool)(value ?? false));
            if (type == typeof(int))
                return EditorGUILayout.IntField(label, (int)(value ?? 0));
            if (type == typeof(float))
                return EditorGUILayout.FloatField(label, (float)(value ?? 0f));
            if (type == typeof(string))
                return EditorGUILayout.TextField(label, (string)(value ?? ""));
            if (type == typeof(double))
                return EditorGUILayout.DoubleField(label, (double)(value ?? 0.0));
            if (type == typeof(long))
                return EditorGUILayout.LongField(label, (long)(value ?? 0L));
            if (type == typeof(Vector2))
                return EditorGUILayout.Vector2Field(label, (Vector2)(value ?? Vector2.zero));
            if (type == typeof(Vector3))
                return EditorGUILayout.Vector3Field(label, (Vector3)(value ?? Vector3.zero));
            if (type == typeof(Vector4))
                return EditorGUILayout.Vector4Field(label, (Vector4)(value ?? Vector4.zero));
            if (type == typeof(Vector2Int))
                return EditorGUILayout.Vector2IntField(label, (Vector2Int)(value ?? Vector2Int.zero));
            if (type == typeof(Vector3Int))
                return EditorGUILayout.Vector3IntField(label, (Vector3Int)(value ?? Vector3Int.zero));
            if (type == typeof(Color))
                return EditorGUILayout.ColorField(label, (Color)(value ?? Color.white));
            if (type == typeof(Rect))
                return EditorGUILayout.RectField(label, (Rect)(value ?? Rect.zero));
            if (type == typeof(RectInt))
                return EditorGUILayout.RectIntField(label, (RectInt)(value ?? new RectInt()));
            if (type == typeof(Bounds))
                return EditorGUILayout.BoundsField(label, (Bounds)(value ?? new Bounds()));
            if (type.IsEnum)
                return EditorGUILayout.EnumPopup(label, (Enum)(value ?? Enum.ToObject(type, 0)));

            EditorGUILayout.HelpBox(
                $"{label}: EditorPrefsValue doesn't support {type.Name}", MessageType.Warning);
            return value;
        }
    }
}
