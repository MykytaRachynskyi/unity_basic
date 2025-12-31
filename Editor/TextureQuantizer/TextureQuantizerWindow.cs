using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Basic;
using UnityEditor;
using UnityEngine;

namespace Basic.UnityEditorTools
{
    public static class ColorExtensions
    {
        /// <summary>
        /// Calculates the Euclidean distance between two colors in RGB space.
        /// </summary>
        public static float DistanceTo(this Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return Mathf.Sqrt(dr * dr + dg * dg + db * db);
        }

        /// <summary>
        /// Calculates the squared distance between two colors in RGB space.
        /// Faster than DistanceTo since it avoids the square root operation.
        /// Use this when you only need to compare distances.
        /// </summary>
        public static float SqrDistanceTo(this Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }
    }

    public class TextureQuantizerWindow : EditorWindow
    {
        private class ColorEqualityComparer : System.Collections.Generic.IEqualityComparer<Color>
        {
            private const float Epsilon = 0.1f;

            public bool Equals(Color x, Color y)
            {
                float distance = x.DistanceTo(y);
                if (distance > _maxColorDistance)
                {
                    _maxColorDistance = distance;
                }
                return distance < Epsilon;
            }

            public int GetHashCode(Color color)
            {
                // Round to nearest 1/255 for consistent hashing
                const float fraction = .9f;
                int r = Mathf.RoundToInt(RoundToFraction(color.r, fraction) * 255f);
                int g = Mathf.RoundToInt(RoundToFraction(color.g, fraction) * 255f);
                int b = Mathf.RoundToInt(RoundToFraction(color.b, fraction) * 255f);
                int a = Mathf.RoundToInt(RoundToFraction(color.a, fraction) * 255f);

                // Combine hash codes
                return (r << 24) | (g << 16) | (b << 8) | a;
            }

            public static float RoundToFraction(float a, float b)
            {
                if (b == 0f)
                    return a;
                return Mathf.Round(a / b) * b;
            }
        }

        private Texture2D sourceTexture;
        private Texture2D paletteTexture;
        private Texture2D quantizedTexture;
        private Vector2 scrollPosition;

        private enum ColorSpace
        {
            RGB,
            LAB,
        }

        private ColorSpace colorSpace = ColorSpace.RGB;

        private Color[] distinctPaletteColors;
        private bool showPaletteColors = false;

        private bool isProcessing = false;
        private string processingStatus = "";
        private float processingProgress = 0f;
        private CancellationTokenSource cancellationTokenSource;

        [MenuItem("Window/Texture Quantizer")]
        public static void ShowWindow()
        {
            GetWindow<TextureQuantizerWindow>("Texture Quantizer");
        }

        private void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

            GUILayout.Label("Texture Quantization Tool", EditorStyles.boldLabel);

            // Reset button
            if (GUILayout.Button("Reset Tool", GUILayout.Height(25)))
            {
                ResetTool();
            }

            EditorGUILayout.Space();

            // Color space dropdown
            EditorGUILayout.LabelField("Color Space", EditorStyles.boldLabel);
            ColorSpace newColorSpace = (ColorSpace)EditorGUILayout.EnumPopup(colorSpace);
            if (newColorSpace != colorSpace)
            {
                colorSpace = newColorSpace;
            }

            EditorGUILayout.Space();

            // Source texture field
            EditorGUILayout.LabelField("Source Texture", EditorStyles.boldLabel);
            Texture2D newSource = (Texture2D)
                EditorGUILayout.ObjectField(sourceTexture, typeof(Texture2D), false);

            if (newSource != sourceTexture)
            {
                sourceTexture = newSource;
            }

            if (sourceTexture != null)
            {
                DrawTexturePreview(sourceTexture);
            }

            EditorGUILayout.Space();

            // Palette texture field
            EditorGUILayout.LabelField("Palette Texture", EditorStyles.boldLabel);
            Texture2D newPalette = (Texture2D)
                EditorGUILayout.ObjectField(paletteTexture, typeof(Texture2D), false);

            if (newPalette != paletteTexture)
            {
                paletteTexture = newPalette;
                distinctPaletteColors = null;
            }

            if (paletteTexture != null)
            {
                DrawTexturePreview(paletteTexture);

                // Foldout to show distinct colors
                if (distinctPaletteColors != null && distinctPaletteColors.Length > 0)
                {
                    showPaletteColors = EditorGUILayout.Foldout(
                        showPaletteColors,
                        $"Distinct Colors ({distinctPaletteColors.Length})"
                    );

                    if (showPaletteColors)
                    {
                        EditorGUI.indentLevel++;

                        for (int i = 0; i < distinctPaletteColors.Length; i++)
                        {
                            EditorGUILayout.BeginHorizontal();
                            EditorGUILayout.ColorField($"Color {i + 1}", distinctPaletteColors[i]);
                            EditorGUILayout.EndHorizontal();
                        }

                        EditorGUI.indentLevel--;
                    }
                }
            }

            EditorGUILayout.Space();

            // Quantize button
            GUI.enabled = !isProcessing;
            if (GUILayout.Button("Quantize Texture", GUILayout.Height(30)))
            {
                if (sourceTexture != null && paletteTexture != null)
                {
                    QuantizeTextureAsync();
                }
                else
                {
                    EditorUtility.DisplayDialog(
                        "Error",
                        "Please assign both source and palette textures.",
                        "OK"
                    );
                }
            }
            GUI.enabled = true;

            // Processing status
            if (isProcessing)
            {
                EditorGUILayout.Space();
                EditorGUILayout.LabelField("Status:", processingStatus);

                Rect progressRect = EditorGUILayout.GetControlRect(false, 20);
                EditorGUI.ProgressBar(
                    progressRect,
                    processingProgress,
                    $"{(int)(processingProgress * 100)}%"
                );

                EditorGUILayout.Space();

                if (GUILayout.Button("Cancel", GUILayout.Height(25)))
                {
                    CancelProcessing();
                }
            }

            EditorGUILayout.Space();

            // Display quantized result
            if (quantizedTexture != null)
            {
                EditorGUILayout.LabelField("Quantized Result", EditorStyles.boldLabel);
                DrawTexturePreview(quantizedTexture);

                EditorGUILayout.Space();

                // Save button
                if (GUILayout.Button("Save Quantized Texture as PNG", GUILayout.Height(30)))
                {
                    SaveTexture();
                }
            }

            EditorGUILayout.EndScrollView();
        }

        private void ResetTool()
        {
            if (isProcessing)
            {
                CancelProcessing();
            }

            sourceTexture = null;
            paletteTexture = null;
            quantizedTexture = null;
            distinctPaletteColors = null;
            colorSpace = ColorSpace.RGB;
            showPaletteColors = false;
            processingStatus = "";
            processingProgress = 0f;

            Debug.Log("Tool reset.");
            Repaint();
        }

        private void DrawTexturePreview(Texture2D texture)
        {
            float maxWidth = position.width - 40;
            float maxHeight = 200;

            float aspectRatio = texture.width / (float)texture.height;
            float width = maxWidth;
            float height = width / aspectRatio;

            if (height > maxHeight)
            {
                height = maxHeight;
                width = height * aspectRatio;
            }

            // Center the texture horizontally
            Rect textureRect = GUILayoutUtility.GetRect(maxWidth, height);
            float xOffset = (maxWidth - width) / 2f;
            textureRect.x += xOffset;
            textureRect.width = width;

            EditorGUI.DrawPreviewTexture(textureRect, texture);
            EditorGUILayout.Space();
        }

        private async void QuantizeTextureAsync()
        {
            if (!IsTextureReadable(sourceTexture))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Source texture is not readable. Please enable Read/Write in texture import settings.",
                    "OK"
                );
                return;
            }

            if (!IsTextureReadable(paletteTexture))
            {
                EditorUtility.DisplayDialog(
                    "Error",
                    "Palette texture is not readable. Please enable Read/Write in texture import settings.",
                    "OK"
                );
                return;
            }

            isProcessing = true;
            processingProgress = 0f;
            cancellationTokenSource = new CancellationTokenSource();

            try
            {
                // Extract distinct colors
                processingStatus = "Extracting palette colors...";
                Repaint();

                // Get palette pixels and filter mode on main thread
                FilterMode originalFilterMode = paletteTexture.filterMode;
                paletteTexture.filterMode = FilterMode.Point;
                Color[] palettePixels = paletteTexture.GetPixels();
                paletteTexture.filterMode = originalFilterMode;

                await Task.Run(() =>
                    ExtractDistinctColorsThreaded(palettePixels, cancellationTokenSource.Token)
                );

                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.Log("Quantization cancelled by user.");
                    return;
                }

                processingProgress = 0.2f;
                Repaint();

                // Prepare data for threading
                processingStatus = "Quantizing texture...";
                Color[] sourcePixels = sourceTexture.GetPixels();
                int width = sourceTexture.width;
                int height = sourceTexture.height;

                Repaint();

                // Quantize pixels in background thread
                Color[] quantizedPixels = await Task.Run(() =>
                    QuantizePixelsThreaded(
                        sourcePixels,
                        distinctPaletteColors,
                        cancellationTokenSource.Token
                    )
                );

                if (cancellationTokenSource.Token.IsCancellationRequested)
                {
                    Debug.Log("Quantization cancelled by user.");
                    return;
                }

                // Create texture on main thread
                processingStatus = "Creating texture...";
                processingProgress = 0.9f;
                Repaint();

                quantizedTexture = new Texture2D(width, height, TextureFormat.RGBA32, false);
                quantizedTexture.SetPixels(quantizedPixels);
                quantizedTexture.Apply();

                processingProgress = 1f;
                processingStatus = "Complete!";

                Debug.Log($"Texture quantized successfully using {colorSpace} color space!");

                await Task.Delay(500);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error during quantization: {e.Message}");
                EditorUtility.DisplayDialog(
                    "Error",
                    $"An error occurred during quantization:\n{e.Message}",
                    "OK"
                );
            }
            finally
            {
                isProcessing = false;
                processingStatus = "";
                processingProgress = 0f;
                cancellationTokenSource?.Dispose();
                cancellationTokenSource = null;
                Repaint();
            }
        }

        private static float _maxColorDistance = float.MinValue;

        private void ExtractDistinctColorsThreaded(Color[] palettePixels, CancellationToken token)
        {
            System.Collections.Generic.Dictionary<Color, int> uniqueColors =
                new System.Collections.Generic.Dictionary<Color, int>(new ColorEqualityComparer());

            _maxColorDistance = 0f;

            for (int i = 0; i < palettePixels.Length; i++)
            {
                if (token.IsCancellationRequested)
                    return;

                if (!uniqueColors.ContainsKey(palettePixels[i]))
                {
                    uniqueColors.Add(palettePixels[i], 1);
                }
                else
                {
                    uniqueColors[palettePixels[i]]++;
                }
            }

            var myList = uniqueColors.ToList();
            myList.Sort((pair1, pair2) => pair1.Value.CompareTo(pair2.Value));
            foreach (var pair in myList)
            {
                Debug.Log($"Color: {pair.Key} - Count: {pair.Value}");
                if (pair.Value < 1900)
                {
                    uniqueColors.Remove(pair.Key);
                }
            }

            distinctPaletteColors = new Color[uniqueColors.Count];
            uniqueColors.Keys.CopyTo(distinctPaletteColors, 0);

            Debug.Log(
                $"Extracted {distinctPaletteColors.Length} distinct colors from palette texture. Max color distance was: {_maxColorDistance}."
            );
        }

        private Color[] QuantizePixelsThreaded(
            Color[] sourcePixels,
            Color[] palette,
            CancellationToken token
        )
        {
            Color[] result = new Color[sourcePixels.Length];
            int totalPixels = sourcePixels.Length;

            System.Threading.Tasks.Parallel.For(
                0,
                totalPixels,
                (i, state) =>
                {
                    if (token.IsCancellationRequested)
                    {
                        state.Stop();
                        return;
                    }

                    result[i] = FindNearestColor(sourcePixels[i], palette);

                    // Update progress periodically
                    if (i % 1000 == 0)
                    {
                        processingProgress = 0.2f + (i / (float)totalPixels) * 0.7f;
                    }
                }
            );

            return result;
        }

        private void CancelProcessing()
        {
            if (cancellationTokenSource != null && !cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
                processingStatus = "Cancelling...";
                Repaint();
            }
        }

        private Color FindNearestColor(Color source, Color[] palette)
        {
            Color nearest = palette[0];
            float minDistance =
                colorSpace == ColorSpace.RGB
                    ? ColorDistanceRGB(source, palette[0])
                    : ColorDistanceLAB(source, palette[0]);

            for (int i = 1; i < palette.Length; i++)
            {
                float distance =
                    colorSpace == ColorSpace.RGB
                        ? ColorDistanceRGB(source, palette[i])
                        : ColorDistanceLAB(source, palette[i]);

                if (distance < minDistance)
                {
                    minDistance = distance;
                    nearest = palette[i];
                }
            }

            return nearest;
        }

        private float ColorDistanceRGB(Color a, Color b)
        {
            float dr = a.r - b.r;
            float dg = a.g - b.g;
            float db = a.b - b.b;
            return dr * dr + dg * dg + db * db;
        }

        private float ColorDistanceLAB(Color a, Color b)
        {
            Vector3 labA = RGBtoLAB(a);
            Vector3 labB = RGBtoLAB(b);

            float dL = labA.x - labB.x;
            float dA = labA.y - labB.y;
            float dB = labA.z - labB.z;

            return dL * dL + dA * dA + dB * dB;
        }

        private Vector3 RGBtoLAB(Color color)
        {
            float r = color.r;
            float g = color.g;
            float b = color.b;

            // Linearize RGB
            r = r > 0.04045f ? Mathf.Pow((r + 0.055f) / 1.055f, 2.4f) : r / 12.92f;
            g = g > 0.04045f ? Mathf.Pow((g + 0.055f) / 1.055f, 2.4f) : g / 12.92f;
            b = b > 0.04045f ? Mathf.Pow((b + 0.055f) / 1.055f, 2.4f) : b / 12.92f;

            // Convert to XYZ (D65 illuminant)
            float x = r * 0.4124564f + g * 0.3575761f + b * 0.1804375f;
            float y = r * 0.2126729f + g * 0.7151522f + b * 0.0721750f;
            float z = r * 0.0193339f + g * 0.1191920f + b * 0.9503041f;

            // Normalize for D65 white point
            x /= 0.95047f;
            y /= 1.00000f;
            z /= 1.08883f;

            // Convert XYZ to LAB
            x = x > 0.008856f ? Mathf.Pow(x, 1f / 3f) : (7.787f * x + 16f / 116f);
            y = y > 0.008856f ? Mathf.Pow(y, 1f / 3f) : (7.787f * y + 16f / 116f);
            z = z > 0.008856f ? Mathf.Pow(z, 1f / 3f) : (7.787f * z + 16f / 116f);

            float L = 116f * y - 16f;
            float A = 500f * (x - y);
            float B = 200f * (y - z);

            return new Vector3(L, A, B);
        }

        private bool IsTextureReadable(Texture2D texture)
        {
            if (texture == null)
                return false;

            try
            {
                texture.GetPixel(0, 0);
                return true;
            }
            catch
            {
                return false;
            }
        }

        private void SaveTexture()
        {
            if (quantizedTexture == null)
            {
                EditorUtility.DisplayDialog("Error", "No quantized texture to save.", "OK");
                return;
            }

            string path = EditorUtility.SaveFilePanel(
                "Save Quantized Texture",
                "",
                "quantized_texture.png",
                "png"
            );

            if (!string.IsNullOrEmpty(path))
            {
                byte[] bytes = quantizedTexture.EncodeToPNG();
                File.WriteAllBytes(path, bytes);
                AssetDatabase.Refresh();
                Debug.Log($"Texture saved to: {path}");
                EditorUtility.DisplayDialog(
                    "Success",
                    $"Texture saved successfully to:\n{path}",
                    "OK"
                );
            }
        }

        private void OnDestroy()
        {
            if (isProcessing && cancellationTokenSource != null)
            {
                cancellationTokenSource.Cancel();
                cancellationTokenSource.Dispose();
            }
        }
    }
}
