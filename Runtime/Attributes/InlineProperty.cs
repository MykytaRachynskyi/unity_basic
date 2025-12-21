using System;
using UnityEditor;
using UnityEngine;

namespace Basic
{
    /// <summary>
    /// Attribute that displays the properties of a serialized object inline,
    /// similar to Odin's InlineProperty attribute.
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class InlinePropertyAttribute : PropertyAttribute { }

#if UNITY_EDITOR
    [CustomPropertyDrawer(typeof(InlinePropertyAttribute))]
    public class InlinePropertyDrawer : PropertyDrawer
    {
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            EditorGUI.BeginProperty(position, label, property);

            // Draw all child properties inline without the foldout
            if (property.hasVisibleChildren)
            {
                float yOffset = 0;

                // Iterate through all visible children
                SerializedProperty child = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();

                child.NextVisible(true); // Enter children

                while (!SerializedProperty.EqualContents(child, endProperty))
                {
                    float height = EditorGUI.GetPropertyHeight(child, true);
                    Rect childRect = new Rect(
                        position.x,
                        position.y + yOffset,
                        position.width,
                        height
                    );

                    EditorGUI.PropertyField(childRect, child, true);

                    yOffset += height + EditorGUIUtility.standardVerticalSpacing;

                    if (!child.NextVisible(false)) // Don't enter children of children
                        break;
                }
            }
            else
            {
                // If no visible children, just draw the property normally
                EditorGUI.PropertyField(position, property, label, true);
            }

            EditorGUI.EndProperty();
        }

        public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
        {
            float totalHeight = 0;

            if (property.hasVisibleChildren)
            {
                SerializedProperty child = property.Copy();
                SerializedProperty endProperty = property.GetEndProperty();

                child.NextVisible(true);

                while (!SerializedProperty.EqualContents(child, endProperty))
                {
                    totalHeight +=
                        EditorGUI.GetPropertyHeight(child, true)
                        + EditorGUIUtility.standardVerticalSpacing;

                    if (!child.NextVisible(false))
                        break;
                }

                // Remove the last spacing
                if (totalHeight > 0)
                    totalHeight -= EditorGUIUtility.standardVerticalSpacing;
            }
            else
            {
                totalHeight = EditorGUI.GetPropertyHeight(property, label, true);
            }

            return totalHeight;
        }
    }
#endif
}
