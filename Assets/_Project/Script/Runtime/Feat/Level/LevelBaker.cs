using System.Collections.Generic;
using UnityEngine;

/// <summary>Chuyển ảnh pixel sang lưới color id theo palette.</summary>
public static class LevelBaker
{
    public struct BakeResult
    {
        public int[] Cells;
        /// <summary>Khoảng cách màu lớn nhất giữa ảnh và palette (để cảnh báo map lệch màu).</summary>
        public float MaxColorDistance;
    }

    /// <param name="pixels">Thứ tự giống Texture2D.GetPixels32: row-major, hàng dưới cùng trước.</param>
    public static BakeResult Bake(Color32[] pixels, int width, int height, PaletteSO palette, byte alphaThreshold = 128)
    {
        var cells = new int[width * height];
        float maxDistance = 0f;
        var cache = new Dictionary<Color32, int>();

        for (int i = 0; i < cells.Length; i++)
        {
            Color32 pixel = pixels[i];
            if (pixel.a < alphaThreshold)
            {
                cells[i] = LevelSO.EmptyCell;
                continue;
            }

            pixel.a = 255;
            if (!cache.TryGetValue(pixel, out int id))
            {
                palette.TryFindNearest(pixel, out id, out float distance);
                if (distance > maxDistance) maxDistance = distance;
                cache[pixel] = id;
            }
            cells[i] = id;
        }

        return new BakeResult { Cells = cells, MaxColorDistance = maxDistance };
    }

    /// <summary>
    /// Lấy đúng các màu Color32 có trong ảnh (giữ nguyên tuyệt đối, không làm tròn về palette chung).
    /// Mỗi màu khác nhau thành một id, theo thứ tự xuất hiện.
    /// </summary>
    public static List<PaletteEntry> ExtractColors(Color32[] pixels, byte alphaThreshold = 128)
    {
        var entries = new List<PaletteEntry>();
        var seen = new HashSet<Color32>();
        foreach (var raw in pixels)
        {
            if (raw.a < alphaThreshold) continue;
            var color = raw;
            color.a = 255;
            if (!seen.Add(color)) continue;
            entries.Add(new PaletteEntry(entries.Count, "#" + ColorUtility.ToHtmlStringRGB(color), color));
        }
        return entries;
    }

    /// <summary>Đếm số pixel của mỗi màu (bỏ ô trống).</summary>
    public static Dictionary<int, int> CountPerColor(IReadOnlyList<int> cells)
    {
        var counts = new Dictionary<int, int>();
        foreach (int id in cells)
        {
            if (id == LevelSO.EmptyCell) continue;
            counts.TryGetValue(id, out int count);
            counts[id] = count + 1;
        }
        return counts;
    }
}
