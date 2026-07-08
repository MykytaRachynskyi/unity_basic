#if UNITY_EDITOR
using System.Collections.Generic;
using Basic.UI;
using UnityEditor;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    [CustomEditor(typeof(BasicImage))]
    [CanEditMultipleObjects]
    public class BasicImageEditor : Editor
    {
        private static readonly string[] OwnProperties =
        {
            "_mode",
            "_localColor",
            "_alpha",
            "_palette",
            "_colorGuid",
            "m_Color",
            "m_Script",
        };

        private SerializedProperty _mode;
        private SerializedProperty _localColor;
        private SerializedProperty _alpha;
        private SerializedProperty _palette;
        private SerializedProperty _colorGuid;

        private void OnEnable()
        {
            _mode = serializedObject.FindProperty("_mode");
            _localColor = serializedObject.FindProperty("_localColor");
            _alpha = serializedObject.FindProperty("_alpha");
            _palette = serializedObject.FindProperty("_palette");
            _colorGuid = serializedObject.FindProperty("_colorGuid");
        }

        public override void OnInspectorGUI()
        {
            serializedObject.Update();

            EditorGUILayout.PropertyField(_mode);

            if (_mode.hasMultipleDifferentValues)
            {
                EditorGUILayout.HelpBox("Select a single tint mode to edit color settings.", MessageType.Info);
            }
            else
            {
                var mode = (PaletteTintMode)_mode.enumValueIndex;
                if (mode == PaletteTintMode.Local)
                    EditorGUILayout.PropertyField(_localColor, new GUIContent("Color"));
                else
                    DrawPaletteFields();
            }

            DrawPropertiesExcluding(serializedObject, OwnProperties);

            if (serializedObject.ApplyModifiedProperties())
                ApplyToTargets();
        }

        private void DrawPaletteFields()
        {
            EditorGUILayout.PropertyField(_palette);
            DrawPaletteColorPopup();
            EditorGUILayout.Slider(_alpha, 0f, 1f, new GUIContent("Alpha"));

            EditorGUI.BeginDisabledGroup(true);
            if (target is BasicImage basicImage)
                EditorGUILayout.ColorField("Resolved Color", basicImage.color);
            EditorGUI.EndDisabledGroup();
        }

        private void DrawPaletteColorPopup()
        {
            if (targets.Length > 1)
            {
                EditorGUILayout.LabelField("Palette Color", "—");
                return;
            }

            var palette = _palette.objectReferenceValue as ColorPalette;
            if (palette == null)
            {
                EditorGUILayout.HelpBox("Assign a palette to select a color.", MessageType.Info);
                return;
            }

            var names = new List<string>();
            palette.GetNames(names);
            if (names.Count == 0)
            {
                EditorGUILayout.HelpBox("Palette has no colors.", MessageType.Warning);
                return;
            }

            var guid = ReadGuid(_colorGuid);
            var index = palette.GUIDToIndex(guid);
            if (index < 0)
                index = 0;

            var newIndex = EditorGUILayout.Popup("Palette Color", index, names.ToArray());
            if (newIndex != index || guid == default)
            {
                var newGuid = palette.IndexToGUID(newIndex);
                WriteGuid(_colorGuid, newGuid);
            }
        }

        private static GUID ReadGuid(SerializedProperty guidProperty)
        {
            return new GUID
            {
                FirstHalf = guidProperty.FindPropertyRelative("FirstHalf").longValue,
                SecondHalf = guidProperty.FindPropertyRelative("SecondHalf").longValue,
            };
        }

        private static void WriteGuid(SerializedProperty guidProperty, GUID guid)
        {
            guidProperty.FindPropertyRelative("FirstHalf").longValue = guid.FirstHalf;
            guidProperty.FindPropertyRelative("SecondHalf").longValue = guid.SecondHalf;
        }

        private void ApplyToTargets()
        {
            foreach (var selected in targets)
            {
                if (selected is not BasicImage basicImage)
                    continue;

                basicImage.ApplyResolvedColor();
                basicImage.RefreshRegistration();
            }
        }
    }
}
#endif
