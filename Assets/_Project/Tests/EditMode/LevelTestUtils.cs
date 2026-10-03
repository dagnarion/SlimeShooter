using System.Collections.Generic;
using UnityEngine;

public static class LevelTestUtils
{
    public const int Red = 0;
    public const int Green = 1;
    public const int Blue = 2;

    public static PaletteSO CreatePalette()
    {
        var palette = ScriptableObject.CreateInstance<PaletteSO>();
        palette.SetEntries(new[]
        {
            new PaletteEntry(Red, "Red", new Color32(230, 40, 40, 255)),
            new PaletteEntry(Green, "Green", new Color32(40, 200, 60, 255)),
            new PaletteEntry(Blue, "Blue", new Color32(40, 80, 230, 255)),
        });
        return palette;
    }

    public static ColumnSpec Column(params (int color, int ammo)[] shooters)
    {
        var column = new ColumnSpec();
        foreach (var (color, ammo) in shooters) column.shooters.Add(new ShooterSpec(color, ammo));
        return column;
    }

    public class FakeSave : ISaveService
    {
        public readonly Dictionary<string, int> Ints = new Dictionary<string, int>();
        public readonly Dictionary<string, string> Strings = new Dictionary<string, string>();
        public int SaveCalls;

        public bool HasKey(string key) => Ints.ContainsKey(key) || Strings.ContainsKey(key);
        public int GetInt(string key, int defaultValue = 0) => Ints.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetInt(string key, int value) => Ints[key] = value;
        public string GetString(string key, string defaultValue = "") => Strings.TryGetValue(key, out var v) ? v : defaultValue;
        public void SetString(string key, string value) => Strings[key] = value;
        public void Delete(string key) { Ints.Remove(key); Strings.Remove(key); }
        public void Save() => SaveCalls++;
    }
}
