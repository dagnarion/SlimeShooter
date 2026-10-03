using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PaletteEntry
{
    public int id;
    public string name;
    public Color32 color;

    public PaletteEntry(int id, string name, Color32 color)
    {
        this.id = id;
        this.name = name;
        this.color = color;
    }
}

[CreateAssetMenu(fileName = "SO_Palette", menuName = "Level/Palette")]
public class PaletteSO : ScriptableObject
{
    [SerializeField] private List<PaletteEntry> entries = new List<PaletteEntry>();

    public IReadOnlyList<PaletteEntry> Entries => entries;

    public void SetEntries(IEnumerable<PaletteEntry> newEntries)
    {
        entries = new List<PaletteEntry>(newEntries);
    }

    public bool Contains(int id) => IndexOf(id) >= 0;

    public bool TryGetEntry(int id, out PaletteEntry entry)
    {
        int index = IndexOf(id);
        entry = index >= 0 ? entries[index] : default;
        return index >= 0;
    }

    public Color32 GetColor(int id)
    {
        return TryGetEntry(id, out var entry) ? entry.color : new Color32(255, 0, 255, 255);
    }

    /// <summary>Tìm màu gần nhất theo khoảng cách "redmean" (gần cảm nhận mắt người hơn RGB thường).</summary>
    public bool TryFindNearest(Color32 color, out int id, out float distance)
    {
        id = -1;
        distance = float.MaxValue;
        foreach (var entry in entries)
        {
            float d = ColorDistance(color, entry.color);
            if (d < distance)
            {
                distance = d;
                id = entry.id;
            }
        }
        return id >= 0;
    }

    public static float ColorDistance(Color32 a, Color32 b)
    {
        int rMean = (a.r + b.r) / 2;
        int dr = a.r - b.r;
        int dg = a.g - b.g;
        int db = a.b - b.b;
        return Mathf.Sqrt((((512 + rMean) * dr * dr) >> 8) + 4 * dg * dg + (((767 - rMean) * db * db) >> 8));
    }

    private int IndexOf(int id)
    {
        for (int i = 0; i < entries.Count; i++)
        {
            if (entries[i].id == id) return i;
        }
        return -1;
    }
}
