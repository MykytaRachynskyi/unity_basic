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
        struct FieldEntry
        {
            public FieldInfo[] OwnerPath;
            public FieldInfo Field;
            public string Key;
        }

        static readonly Dictionary<string, object> _cache = new();
        static readonly Dictionary<Type, FieldEntry[]> _entriesPerType = new();

        static EditorPrefsValueDrawer()
        {
            NaughtyInspector.RegisterAdditionalDrawer(DrawTarget);
        }

        static FieldEntry[] GetEntries(Type type)
        {
            if (!_entriesPerType.TryGetValue(type, out var entries))
            {
                var list = new List<FieldEntry>();
                CollectFields(type, Array.Empty<FieldInfo>(), list, new HashSet<Type>());
                entries = list.ToArray();
                _entriesPerType[type] = entries;
            }
            return entries;
        }

        static void CollectFields(Type type, FieldInfo[] pathSoFar, List<FieldEntry> results, HashSet<Type> visited)
        {
            if (type == null || !visited.Add(type))
                return;

            var t = type;
            while (t != null)
            {
                foreach (var f in t.GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.DeclaredOnly))
                {
                    var attr = f.GetCustomAttribute<EditorPrefsValueAttribute>();
                    if (attr != null)
                    {
                        results.Add(new FieldEntry { OwnerPath = pathSoFar, Field = f, Key = attr.Key });
                    }
                    else if (f.FieldType.IsClass || (f.FieldType.IsValueType && !f.FieldType.IsPrimitive && !f.FieldType.IsEnum))
                    {
                        if (f.FieldType.GetCustomAttribute<SerializableAttribute>() != null
                            && !f.FieldType.Namespace?.StartsWith("System") == true
                            && !f.FieldType.Namespace?.StartsWith("UnityEngine") == true)
                        {
                            var newPath = new FieldInfo[pathSoFar.Length + 1];
                            pathSoFar.CopyTo(newPath, 0);
                            newPath[pathSoFar.Length] = f;
                            CollectFields(f.FieldType, newPath, results, visited);
                        }
                    }
                }
                t = t.BaseType;
            }
        }

        static object ResolveOwner(object root, FieldInfo[] path)
        {
            object current = root;
            foreach (var f in path)
            {
                current = f.GetValue(current);
                if (current == null) return null;
            }
            return current;
        }

        static void DrawTarget(UnityEngine.Object target)
        {
            var entries = GetEntries(target.GetType());
            foreach (var entry in entries)
            {
                var owner = ResolveOwner(target, entry.OwnerPath);
                if (owner != null)
                    DrawField(owner, entry.Field, entry.Key);
            }
        }

        static void DrawField(object owner, FieldInfo field, string key)
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
                        _cache[key] = field.GetValue(owner);
                    }
                }
                else
                {
                    _cache[key] = field.GetValue(owner);
                }
            }

            field.SetValue(owner, _cache[key]);

            string label = ObjectNames.NicifyVariableName(field.Name);
            object current = _cache[key];
            object newValue = DrawEditableField(current, field.FieldType, label);

            if (!Equals(current, newValue))
            {
                _cache[key] = newValue;
                field.SetValue(owner, newValue);
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
