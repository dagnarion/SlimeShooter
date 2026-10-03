using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Pipeline ảnh -> LevelSO dùng chung cho LevelGeneratorWindow và script batch.</summary>
public static class LevelAssetBuilder
{
    public struct Options
    {
        public PaletteSO Palette;
        public int ColumnCount;
        public int[] AmmoSteps;
        public int Seed;
        public int ConveyorSlots;
        public int CacheSlots;
        public byte AlphaThreshold;

        public static Options Default(PaletteSO palette) => new Options
        {
            Palette = palette,
            ColumnCount = 4,
            AmmoSteps = new[] { 10, 20, 30 },
            Seed = 1,
            ConveyorSlots = 5,
            CacheSlots = 5,
            AlphaThreshold = 128
        };
    }

    public struct Preview
    {
        public int Width;
        public int Height;
        public LevelBaker.BakeResult Bake;
        public System.Collections.Generic.List<ColumnSpec> Columns;
        public LevelValidator.Result Validation;
    }

    /// <summary>Ảnh nguồn phải đọc được pixel và không bị nén/lọc làm lệch màu.</summary>
    public static void EnsureImportSettings(Texture2D texture)
    {
        string path = AssetDatabase.GetAssetPath(texture);
        if (!(AssetImporter.GetAtPath(path) is TextureImporter importer)) return;

        bool changed = !importer.isReadable
                       || importer.filterMode != FilterMode.Point
                       || importer.textureCompression != TextureImporterCompression.Uncompressed
                       || importer.mipmapEnabled
                       || importer.npotScale != TextureImporterNPOTScale.None;
        if (!changed) return;

        importer.isReadable = true;
        importer.filterMode = FilterMode.Point;
        importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false;
        importer.npotScale = TextureImporterNPOTScale.None;
        importer.SaveAndReimport();
    }

    public static Preview BuildPreview(Texture2D texture, Options options)
    {
        EnsureImportSettings(texture);

        var bake = LevelBaker.Bake(texture.GetPixels32(), texture.width, texture.height, options.Palette, options.AlphaThreshold);
        var columns = ShooterGenerator.Generate(LevelBaker.CountPerColor(bake.Cells), new ShooterGenerator.Settings
        {
            ColumnCount = options.ColumnCount,
            AmmoSteps = options.AmmoSteps,
            Seed = options.Seed
        });
        var validation = LevelValidator.Validate(texture.width, texture.height, bake.Cells, columns, options.Palette,
            options.ConveyorSlots, options.CacheSlots);

        return new Preview { Width = texture.width, Height = texture.height, Bake = bake, Columns = columns, Validation = validation };
    }

    public static void Apply(LevelSO level, Texture2D texture, Options options, Preview preview)
    {
        Undo.RecordObject(level, "Generate Level");
        level.SetBoard(options.Palette, preview.Width, preview.Height, preview.Bake.Cells);
        level.SetColumns(preview.Columns, options.ConveyorSlots, options.CacheSlots);
        level.SetGeneratorParams(AssetDatabase.GetAssetPath(texture), options.ColumnCount, options.AmmoSteps, options.Seed);
        EditorUtility.SetDirty(level);
    }

    /// <summary>Tạo mới (hoặc ghi đè) LevelSO tại assetPath từ ảnh.</summary>
    public static LevelSO CreateOrUpdate(string assetPath, Texture2D texture, Options options, out Preview preview)
    {
        preview = BuildPreview(texture, options);

        var level = AssetDatabase.LoadAssetAtPath<LevelSO>(assetPath);
        if (level == null)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(assetPath) ?? "Assets");
            level = ScriptableObject.CreateInstance<LevelSO>();
            AssetDatabase.CreateAsset(level, assetPath);
        }

        Apply(level, texture, options, preview);
        AssetDatabase.SaveAssets();
        return level;
    }
}
