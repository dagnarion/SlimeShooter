using System.IO;
using UnityEditor;
using UnityEngine;

/// <summary>Pipeline ảnh -> LevelSO dùng chung cho LevelGeneratorWindow và batch.</summary>
public static class LevelAssetBuilder
{
    public struct Options
    {
        /// <summary>Palette chung (khi ExactColors = false).</summary>
        public PaletteSO Palette;
        /// <summary>true: giữ đúng màu Color32 của ảnh (palette riêng lưu trong level), không làm tròn về palette chung.</summary>
        public bool ExactColors;
        public int ColumnCount;
        public int[] AmmoSteps;
        /// <summary>> 0: tự tính bậc ammo để có khoảng chừng này shooter (bỏ qua AmmoSteps).</summary>
        public int TargetShooterCount;
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
        /// <summary>Palette thực sự dùng (palette chung hoặc palette tạm sinh từ ảnh).</summary>
        public PaletteSO Palette;
        public bool PaletteIsTemporary;
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
        Color32[] pixels = texture.GetPixels32();

        PaletteSO palette = options.Palette;
        bool temporary = false;
        if (options.ExactColors)
        {
            palette = ScriptableObject.CreateInstance<PaletteSO>();
            palette.name = texture.name + "_Palette";
            palette.SetEntries(LevelBaker.ExtractColors(pixels, options.AlphaThreshold));
            temporary = true;
        }

        var bake = LevelBaker.Bake(pixels, texture.width, texture.height, palette, options.AlphaThreshold);
        var columns = ShooterGenerator.Generate(LevelBaker.CountPerColor(bake.Cells), new ShooterGenerator.Settings
        {
            ColumnCount = options.ColumnCount,
            AmmoSteps = options.AmmoSteps,
            TargetShooterCount = options.TargetShooterCount,
            Seed = options.Seed
        });
        var validation = LevelValidator.Validate(texture.width, texture.height, bake.Cells, columns, palette,
            options.ConveyorSlots, options.CacheSlots);

        return new Preview
        {
            Width = texture.width, Height = texture.height, Palette = palette, PaletteIsTemporary = temporary,
            Bake = bake, Columns = columns, Validation = validation
        };
    }

    public static void Apply(LevelSO level, Texture2D texture, Options options, Preview preview)
    {
        Undo.RecordObject(level, "Generate Level");

        PaletteSO palette = preview.Palette;
        if (preview.PaletteIsTemporary) palette = StoreEmbeddedPalette(level, preview.Palette);

        level.SetBoard(palette, preview.Width, preview.Height, preview.Bake.Cells);
        level.SetColumns(preview.Columns, options.ConveyorSlots, options.CacheSlots);
        level.SetGeneratorParams(AssetDatabase.GetAssetPath(texture), options.ColumnCount, options.AmmoSteps, options.Seed);
        EditorUtility.SetDirty(level);
    }

    /// <summary>Lưu palette màu chính xác làm sub-asset bên trong file level (mỗi level mang palette riêng).</summary>
    private static PaletteSO StoreEmbeddedPalette(LevelSO level, PaletteSO temporary)
    {
        string path = AssetDatabase.GetAssetPath(level);
        PaletteSO embedded = null;
        foreach (var sub in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
        {
            if (sub is PaletteSO existing) { embedded = existing; break; }
        }
        if (embedded == null)
        {
            embedded = ScriptableObject.CreateInstance<PaletteSO>();
            embedded.name = level.name + "_Palette";
            AssetDatabase.AddObjectToAsset(embedded, level);
        }
        embedded.SetEntries(temporary.Entries);
        EditorUtility.SetDirty(embedded);
        Object.DestroyImmediate(temporary);
        return embedded;
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
