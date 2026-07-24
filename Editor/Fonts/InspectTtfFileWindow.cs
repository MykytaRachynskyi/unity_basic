using System;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    public class InspectTtfFileWindow : EditorWindow
    {
        private Font _font;
        private string _dump = string.Empty;
        private string _error = string.Empty;
        private int _characterCount;
        private Vector2 _scroll;

        [MenuItem("Tools/Basic/Fonts/Inspect TTF File")]
        public static void Open()
        {
            var window = GetWindow<InspectTtfFileWindow>("Inspect TTF File");
            window.minSize = new Vector2(420f, 320f);
            window.Show();
        }

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Font file (.ttf / .otf)", EditorStyles.boldLabel);
            _font = (Font)EditorGUILayout.ObjectField(_font, typeof(Font), false);

            using (new EditorGUILayout.HorizontalScope())
            {
                using (new EditorGUI.DisabledScope(_font == null))
                {
                    if (GUILayout.Button("Dump Characters", GUILayout.Height(24f)))
                        DumpCharacters();
                }

                using (new EditorGUI.DisabledScope(string.IsNullOrEmpty(_dump)))
                {
                    if (GUILayout.Button("Copy to Clipboard", GUILayout.Height(24f)))
                        EditorGUIUtility.systemCopyBuffer = _dump;
                }
            }

            if (!string.IsNullOrEmpty(_error))
                EditorGUILayout.HelpBox(_error, MessageType.Error);
            else if (_characterCount > 0 || !string.IsNullOrEmpty(_dump))
                EditorGUILayout.LabelField($"{_characterCount} characters");

            _scroll = EditorGUILayout.BeginScrollView(_scroll);
            EditorGUILayout.TextArea(_dump, GUILayout.ExpandHeight(true));
            EditorGUILayout.EndScrollView();
        }

        private void DumpCharacters()
        {
            _error = string.Empty;
            _dump = string.Empty;
            _characterCount = 0;

            if (_font == null)
            {
                _error = "Assign a font file.";
                return;
            }

            var assetPath = AssetDatabase.GetAssetPath(_font);
            if (!FontCharacterSet.IsSupportedFontFilePath(assetPath))
            {
                _error = "Unsupported file. Assign a .ttf or .otf asset.";
                return;
            }

            var absolutePath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), assetPath));
            if (!File.Exists(absolutePath))
            {
                _error = $"File not found:\n{absolutePath}";
                return;
            }

            try
            {
                var fontData = File.ReadAllBytes(absolutePath);
                _dump = FontCharacterSet.DumpFromFontData(fontData, out _characterCount);
            }
            catch (Exception exception)
            {
                _error = exception.Message;
            }
        }
    }
}
