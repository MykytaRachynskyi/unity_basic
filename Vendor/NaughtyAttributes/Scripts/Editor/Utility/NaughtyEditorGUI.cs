using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEditor.Experimental.SceneManagement;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace NaughtyAttributes.Editor
{
    public static class NaughtyEditorGUI
    {
        public const float IndentLength = 15.0f;
        public const float HorizontalSpacing = 2.0f;

        private static GUIStyle _buttonStyle = new GUIStyle(GUI.skin.button) { richText = true };

        private delegate void PropertyFieldFunction(Rect rect, SerializedProperty property, GUIContent label, bool includeChildren);

        public static void PropertyField(Rect rect, SerializedProperty property, bool includeChildren)
        {
            PropertyField_Implementation(rect, property, includeChildren, DrawPropertyField);
        }

        public static void PropertyField_Layout(SerializedProperty property, bool includeChildren)
        {
            Rect dummyRect = new Rect();
            PropertyField_Implementation(dummyRect, property, includeChildren, DrawPropertyField_Layout);
        }

        private static void DrawPropertyField(Rect rect, SerializedProperty property, GUIContent label, bool includeChildren)
        {
            EditorGUI.PropertyField(rect, property, label, includeChildren);
        }

        private static void DrawPropertyField_Layout(Rect rect, SerializedProperty property, GUIContent label, bool includeChildren)
        {
            EditorGUILayout.PropertyField(property, label, includeChildren);
        }

        private static void PropertyField_Implementation(Rect rect, SerializedProperty property, bool includeChildren, PropertyFieldFunction propertyFieldFunction)
        {
            SpecialCaseDrawerAttribute specialCaseAttribute = PropertyUtility.GetAttribute<SpecialCaseDrawerAttribute>(property);
            if (specialCaseAttribute != null)
            {
                specialCaseAttribute.GetDrawer().OnGUI(rect, property);
            }
            else
            {
                // Check if visible
                bool visible = PropertyUtility.IsVisible(property);
                if (!visible)
                {
                    return;
                }

                // Validate
                ValidatorAttribute[] validatorAttributes = PropertyUtility.GetAttributes<ValidatorAttribute>(property);
                foreach (var validatorAttribute in validatorAttributes)
                {
                    validatorAttribute.GetValidator().ValidateProperty(property);
                }

                // Check if enabled and draw
                EditorGUI.BeginChangeCheck();
                bool enabled = PropertyUtility.IsEnabled(property);

                using (new EditorGUI.DisabledScope(disabled: !enabled))
                {
                    propertyFieldFunction.Invoke(rect, property, PropertyUtility.GetLabel(property), includeChildren);
                }

                // Call OnValueChanged callbacks
                if (EditorGUI.EndChangeCheck())
                {
                    PropertyUtility.CallOnValueChangedCallbacks(property);
                }
            }
        }

        public static float GetIndentLength(Rect sourceRect)
        {
            Rect indentRect = EditorGUI.IndentedRect(sourceRect);
            float indentLength = indentRect.x - sourceRect.x;

            return indentLength;
        }

        public static void BeginBoxGroup_Layout(string label = "")
        {
            EditorGUILayout.BeginVertical(GUI.skin.box);
            if (!string.IsNullOrEmpty(label))
            {
                EditorGUILayout.LabelField(label, EditorStyles.boldLabel);
            }
        }

        public static void EndBoxGroup_Layout()
        {
            EditorGUILayout.EndVertical();
        }

        /// <summary>
        /// Creates a dropdown. Writes through <see cref="SerializedProperty"/> so ScriptableObject
        /// assets are dirtied and persist to disk (reflection-only SetValue does not).
        /// </summary>
        /// <param name="rect">The rect the defines the position and size of the dropdown in the inspector</param>
        /// <param name="property">The serialized property being edited</param>
        /// <param name="label">The label of the dropdown</param>
        /// <param name="selectedValueIndex">The index of the value from the values array</param>
        /// <param name="values">The values of the dropdown</param>
        /// <param name="displayOptions">The display options for the values</param>
        public static void Dropdown(
            Rect rect, SerializedProperty property,
            string label, int selectedValueIndex, object[] values, string[] displayOptions)
        {
            int newIndex = EditorGUI.Popup(rect, label, selectedValueIndex, displayOptions);
            object newValue = values[newIndex];

            if (SerializedPropertyEquals(property, newValue))
            {
                return;
            }

            Undo.RecordObject(property.serializedObject.targetObject, "Dropdown");
            SetSerializedPropertyValue(property, newValue);
            EditorUtility.SetDirty(property.serializedObject.targetObject);
        }

        private static bool SerializedPropertyEquals(SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    return value != null && property.intValue.Equals(Convert.ToInt32(value));
                case SerializedPropertyType.Boolean:
                    return value != null && property.boolValue.Equals(Convert.ToBoolean(value));
                case SerializedPropertyType.Float:
                    return value != null && property.floatValue.Equals(Convert.ToSingle(value));
                case SerializedPropertyType.String:
                    var stringValue = value as string ?? value?.ToString() ?? string.Empty;
                    return property.stringValue == stringValue;
                case SerializedPropertyType.ObjectReference:
                    return property.objectReferenceValue == value as UnityEngine.Object;
                case SerializedPropertyType.Enum:
                    return value != null && property.intValue.Equals(Convert.ToInt32(value));
                case SerializedPropertyType.Vector2:
                    return value is Vector2 vector2 && property.vector2Value == vector2;
                case SerializedPropertyType.Vector3:
                    return value is Vector3 vector3 && property.vector3Value == vector3;
                case SerializedPropertyType.Vector4:
                    return value is Vector4 vector4 && property.vector4Value == vector4;
                case SerializedPropertyType.Color:
                    return value is Color color && property.colorValue == color;
                case SerializedPropertyType.Quaternion:
                    return value is Quaternion quaternion && property.quaternionValue == quaternion;
                default:
                    return false;
            }
        }

        private static void SetSerializedPropertyValue(SerializedProperty property, object value)
        {
            switch (property.propertyType)
            {
                case SerializedPropertyType.Integer:
                    property.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Boolean:
                    property.boolValue = Convert.ToBoolean(value);
                    break;
                case SerializedPropertyType.Float:
                    property.floatValue = Convert.ToSingle(value);
                    break;
                case SerializedPropertyType.String:
                    property.stringValue = value as string ?? value?.ToString() ?? string.Empty;
                    break;
                case SerializedPropertyType.ObjectReference:
                    property.objectReferenceValue = value as UnityEngine.Object;
                    break;
                case SerializedPropertyType.Enum:
                    property.intValue = Convert.ToInt32(value);
                    break;
                case SerializedPropertyType.Vector2:
                    property.vector2Value = (Vector2)value;
                    break;
                case SerializedPropertyType.Vector3:
                    property.vector3Value = (Vector3)value;
                    break;
                case SerializedPropertyType.Vector4:
                    property.vector4Value = (Vector4)value;
                    break;
                case SerializedPropertyType.Color:
                    property.colorValue = (Color)value;
                    break;
                case SerializedPropertyType.Quaternion:
                    property.quaternionValue = (Quaternion)value;
                    break;
                default:
                    Debug.LogWarning(
                        $"Dropdown cannot write SerializedPropertyType.{property.propertyType} on '{property.propertyPath}'.",
                        property.serializedObject.targetObject);
                    break;
            }
        }

        public static void Button(UnityEngine.Object target, MethodInfo methodInfo)
        {
            bool visible = ButtonUtility.IsVisible(target, methodInfo);
            if (!visible)
            {
                return;
            }

            if (methodInfo.GetParameters().All(p => p.IsOptional))
            {
                ButtonAttribute buttonAttribute = (ButtonAttribute)methodInfo.GetCustomAttributes(typeof(ButtonAttribute), true)[0];
                string buttonText = string.IsNullOrEmpty(buttonAttribute.Text) ? ObjectNames.NicifyVariableName(methodInfo.Name) : buttonAttribute.Text;

                bool buttonEnabled = ButtonUtility.IsEnabled(target, methodInfo);

                EButtonEnableMode mode = buttonAttribute.SelectedEnableMode;
                buttonEnabled &=
                    mode == EButtonEnableMode.Always ||
                    mode == EButtonEnableMode.Editor && !Application.isPlaying ||
                    mode == EButtonEnableMode.Playmode && Application.isPlaying;

                bool methodIsCoroutine = methodInfo.ReturnType == typeof(IEnumerator);
                if (methodIsCoroutine)
                {
                    buttonEnabled &= (Application.isPlaying ? true : false);
                }

                EditorGUI.BeginDisabledGroup(!buttonEnabled);

                if (GUILayout.Button(buttonText, _buttonStyle))
                {
                    object[] defaultParams = methodInfo.GetParameters().Select(p => p.DefaultValue).ToArray();
                    IEnumerator methodResult = methodInfo.Invoke(target, defaultParams) as IEnumerator;

                    if (!Application.isPlaying)
                    {
                        // Set target object and scene dirty to serialize changes to disk
                        EditorUtility.SetDirty(target);

                        PrefabStage stage = PrefabStageUtility.GetCurrentPrefabStage();
                        if (stage != null)
                        {
                            // Prefab mode
                            EditorSceneManager.MarkSceneDirty(stage.scene);
                        }
                        else
                        {
                            // Normal scene
                            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
                        }
                    }
                    else if (methodResult != null && target is MonoBehaviour behaviour)
                    {
                        behaviour.StartCoroutine(methodResult);
                    }
                }

                EditorGUI.EndDisabledGroup();
            }
            else
            {
                string warning = typeof(ButtonAttribute).Name + " works only on methods with no parameters";
                HelpBox_Layout(warning, MessageType.Warning, context: target, logToConsole: true);
            }
        }

        public static void NativeProperty_Layout(UnityEngine.Object target, PropertyInfo property)
        {
            object value = property.GetValue(target, null);

            if (value == null)
            {
                string warning = string.Format("{0} is null. {1} doesn't support reference types with null value", ObjectNames.NicifyVariableName(property.Name), typeof(ShowNativePropertyAttribute).Name);
                HelpBox_Layout(warning, MessageType.Warning, context: target);
            }
            else if (!Field_Layout(value, ObjectNames.NicifyVariableName(property.Name)))
            {
                string warning = string.Format("{0} doesn't support {1} types", typeof(ShowNativePropertyAttribute).Name, property.PropertyType.Name);
                HelpBox_Layout(warning, MessageType.Warning, context: target);
            }
        }

        public static void NonSerializedField_Layout(UnityEngine.Object target, FieldInfo field)
        {
            object value = field.GetValue(target);

            if (value == null)
            {
                string warning = string.Format("{0} is null. {1} doesn't support reference types with null value", ObjectNames.NicifyVariableName(field.Name), typeof(ShowNonSerializedFieldAttribute).Name);
                HelpBox_Layout(warning, MessageType.Warning, context: target);
            }
            else if (!Field_Layout(value, ObjectNames.NicifyVariableName(field.Name)))
            {
                string warning = string.Format("{0} doesn't support {1} types", typeof(ShowNonSerializedFieldAttribute).Name, field.FieldType.Name);
                HelpBox_Layout(warning, MessageType.Warning, context: target);
            }
        }

        public static void HorizontalLine(Rect rect, float height, Color color)
        {
            rect.height = height;
            EditorGUI.DrawRect(rect, color);
        }

        public static void HelpBox(Rect rect, string message, MessageType type, UnityEngine.Object context = null, bool logToConsole = false)
        {
            EditorGUI.HelpBox(rect, message, type);

            if (logToConsole)
            {
                DebugLogMessage(message, type, context);
            }
        }

        public static void HelpBox_Layout(string message, MessageType type, UnityEngine.Object context = null, bool logToConsole = false)
        {
            EditorGUILayout.HelpBox(message, type);

            if (logToConsole)
            {
                DebugLogMessage(message, type, context);
            }
        }

        public static bool Field_Layout(object value, string label)
        {
            using (new EditorGUI.DisabledScope(disabled: true))
            {
                bool isDrawn = true;
                Type valueType = value.GetType();

                if (valueType == typeof(bool))
                {
                    EditorGUILayout.Toggle(label, (bool)value);
                }
                else if (valueType == typeof(short))
                {
                    EditorGUILayout.IntField(label, (short)value);
                }
                else if (valueType == typeof(ushort))
                {
                    EditorGUILayout.IntField(label, (ushort)value);
                }
                else if (valueType == typeof(int))
                {
                    EditorGUILayout.IntField(label, (int)value);
                }
                else if (valueType == typeof(uint))
                {
                    EditorGUILayout.LongField(label, (uint)value);
                }
                else if (valueType == typeof(long))
                {
                    EditorGUILayout.LongField(label, (long)value);
                }
                else if (valueType == typeof(ulong))
                {
                    EditorGUILayout.TextField(label, ((ulong)value).ToString());
                }
                else if (valueType == typeof(float))
                {
                    EditorGUILayout.FloatField(label, (float)value);
                }
                else if (valueType == typeof(double))
                {
                    EditorGUILayout.DoubleField(label, (double)value);
                }
                else if (valueType == typeof(string))
                {
                    EditorGUILayout.TextField(label, (string)value);
                }
                else if (valueType == typeof(Vector2))
                {
                    EditorGUILayout.Vector2Field(label, (Vector2)value);
                }
                else if (valueType == typeof(Vector3))
                {
                    EditorGUILayout.Vector3Field(label, (Vector3)value);
                }
                else if (valueType == typeof(Vector4))
                {
                    EditorGUILayout.Vector4Field(label, (Vector4)value);
                }
                else if (valueType == typeof(Vector2Int))
                {
                    EditorGUILayout.Vector2IntField(label, (Vector2Int)value);
                }
                else if (valueType == typeof(Vector3Int))
                {
                    EditorGUILayout.Vector3IntField(label, (Vector3Int)value);
                }
                else if (valueType == typeof(Color))
                {
                    EditorGUILayout.ColorField(label, (Color)value);
                }
                else if (valueType == typeof(Bounds))
                {
                    EditorGUILayout.BoundsField(label, (Bounds)value);
                }
                else if (valueType == typeof(Rect))
                {
                    EditorGUILayout.RectField(label, (Rect)value);
                }
                else if (valueType == typeof(RectInt))
                {
                    EditorGUILayout.RectIntField(label, (RectInt)value);
                }
                else if (typeof(UnityEngine.Object).IsAssignableFrom(valueType))
                {
                    EditorGUILayout.ObjectField(label, (UnityEngine.Object)value, valueType, true);
                }
                else if (valueType.BaseType == typeof(Enum))
                {
                    EditorGUILayout.EnumPopup(label, (Enum)value);
                }
                else if (valueType.BaseType == typeof(System.Reflection.TypeInfo))
                {
                    EditorGUILayout.TextField(label, value.ToString());
                }
                else
                {
                    isDrawn = false;
                }

                return isDrawn;
            }
        }

        private static void DebugLogMessage(string message, MessageType type, UnityEngine.Object context)
        {
            switch (type)
            {
                case MessageType.None:
                case MessageType.Info:
                    Debug.Log(message, context);
                    break;
                case MessageType.Warning:
                    Debug.LogWarning(message, context);
                    break;
                case MessageType.Error:
                    Debug.LogError(message, context);
                    break;
            }
        }
    }
}
