using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[CustomEditor(typeof(SpawnDataSO))]
public class SpawnDataSOEditor : Editor
{
    private List<Color32> _uniqueColors;
    private Vector2 _scrollPos;
    private string _lastTextureName;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        SpawnDataSO data = (SpawnDataSO)target;

        EditorGUILayout.Space();

        if (GUILayout.Button("Count Unique Colors"))
        {
            if (data.SpawnTexture == null)
            {
                Debug.LogWarning("No SpawnTexture assigned.");
                _uniqueColors = null;
            }
            else
            {
                _uniqueColors = GetUniqueColors(data.SpawnTexture);
                _lastTextureName = data.SpawnTexture.name;
            }
        }

        if (_uniqueColors != null)
        {
            EditorGUILayout.Space();
            EditorGUILayout.LabelField($"Texture: {_lastTextureName}", EditorStyles.boldLabel);
            EditorGUILayout.LabelField($"Unique Colors: {_uniqueColors.Count}", EditorStyles.boldLabel);
            EditorGUILayout.Space();

            _scrollPos = EditorGUILayout.BeginScrollView(_scrollPos, GUILayout.Height(300));

            foreach (Color32 c in _uniqueColors)
            {
                EditorGUILayout.BeginHorizontal();

                // Color swatch
                Rect swatchRect = GUILayoutUtility.GetRect(20, 16, GUILayout.Width(20));
                EditorGUI.DrawRect(swatchRect, new Color(c.r / 255f, c.g / 255f, c.b / 255f, 1f));

                // Hex label (selectable so it can be copied)
                string hex = ColorToHex(c);
                EditorGUILayout.SelectableLabel(hex, GUILayout.Height(16));

                EditorGUILayout.EndHorizontal();
            }

            EditorGUILayout.EndScrollView();

            if (GUILayout.Button("Copy All Hex Values"))
            {
                System.Text.StringBuilder sb = new System.Text.StringBuilder();
                foreach (Color32 c in _uniqueColors)
                {
                    sb.AppendLine(ColorToHex(c));
                }
                EditorGUIUtility.systemCopyBuffer = sb.ToString();
                Debug.Log($"Copied {_uniqueColors.Count} hex values to clipboard.");
            }
        }
    }

    private static List<Color32> GetUniqueColors(Texture2D sourceTexture)
    {
        Texture2D readableTexture = GetReadableTexture(sourceTexture);

        Color32[] pixels = readableTexture.GetPixels32();
        HashSet<Color32> uniqueSet = new HashSet<Color32>();

        foreach (Color32 pixel in pixels)
        {
            uniqueSet.Add(pixel);
        }

        if (readableTexture != sourceTexture)
        {
            Object.DestroyImmediate(readableTexture);
        }

        List<Color32> sortedColors = new List<Color32>(uniqueSet);
        sortedColors.Sort((a, b) => ColorToHexInt(a).CompareTo(ColorToHexInt(b)));

        return sortedColors;
    }

    private static string ColorToHex(Color32 c)
    {
        return $"#{c.r:X2}{c.g:X2}{c.b:X2}{c.a:X2}";
    }

    private static int ColorToHexInt(Color32 c)
    {
        return (c.r << 24) | (c.g << 16) | (c.b << 8) | c.a;
    }

    /// <summary>
    /// Returns a readable copy of the texture (works even if Read/Write is disabled
    /// in the import settings), using a temporary RenderTexture + ReadPixels.
    /// </summary>
    private static Texture2D GetReadableTexture(Texture2D source)
    {
        if (source.isReadable)
        {
            return source;
        }

        RenderTexture rt = RenderTexture.GetTemporary(
            source.width,
            source.height,
            0,
            RenderTextureFormat.Default,
            RenderTextureReadWrite.Linear);

        Graphics.Blit(source, rt);

        RenderTexture previous = RenderTexture.active;
        RenderTexture.active = rt;

        Texture2D readable = new Texture2D(source.width, source.height, TextureFormat.RGBA32, false);
        readable.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0);
        readable.Apply();

        RenderTexture.active = previous;
        RenderTexture.ReleaseTemporary(rt);

        return readable;
    }
}