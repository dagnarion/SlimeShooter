using System.Collections.Generic;
using System.Linq;

public static class LevelValidator
{
    public class Result
    {
        public readonly List<string> Errors = new List<string>();
        public bool IsValid => Errors.Count == 0;
        public override string ToString() => IsValid ? "OK" : string.Join("\n", Errors);
    }

    public static Result Validate(LevelSO level)
    {
        if (level == null)
        {
            var result = new Result();
            result.Errors.Add("Level null");
            return result;
        }
        return Validate(level.Width, level.Height, level.Cells, level.Columns, level.Palette, level.ConveyorSlots, level.CacheSlots);
    }

    /// <param name="palette">null = bỏ qua kiểm tra màu có trong palette.</param>
    public static Result Validate(int width, int height, IReadOnlyList<int> cells, IReadOnlyList<ColumnSpec> columns,
        PaletteSO palette, int conveyorSlots, int cacheSlots)
    {
        var result = new Result();
        var errors = result.Errors;

        if (width <= 0 || height <= 0) errors.Add($"Kích thước lưới không hợp lệ: {width}x{height}");
        if (cells == null || cells.Count != width * height)
        {
            errors.Add($"Số ô ({cells?.Count ?? 0}) khác {width}x{height}");
            return result;
        }
        if (conveyorSlots < 1) errors.Add("conveyorSlots phải >= 1");
        if (cacheSlots < 1) errors.Add("cacheSlots phải >= 1");

        var pixels = LevelBaker.CountPerColor(cells);
        if (pixels.Count == 0) errors.Add("Lưới không có pixel nào");

        var ammo = new Dictionary<int, int>();
        if (columns == null || columns.Count == 0)
        {
            errors.Add("Không có cột shooter nào");
        }
        else
        {
            for (int c = 0; c < columns.Count; c++)
            {
                var shooters = columns[c]?.shooters;
                if (shooters == null || shooters.Count == 0)
                {
                    errors.Add($"Cột {c} rỗng");
                    continue;
                }
                for (int s = 0; s < shooters.Count; s++)
                {
                    var spec = shooters[s];
                    if (spec.ammo <= 0) errors.Add($"Cột {c}, shooter {s}: ammo phải > 0 (đang là {spec.ammo})");
                    ammo.TryGetValue(spec.colorId, out int sum);
                    ammo[spec.colorId] = sum + spec.ammo;
                }
            }
        }

        if (palette != null)
        {
            foreach (int id in pixels.Keys.Where(id => !palette.Contains(id)))
                errors.Add($"Màu {id} trên lưới không có trong palette");
            foreach (int id in ammo.Keys.Where(id => !palette.Contains(id)))
                errors.Add($"Màu {id} của shooter không có trong palette");
        }

        foreach (int id in pixels.Keys.Union(ammo.Keys).OrderBy(id => id))
        {
            pixels.TryGetValue(id, out int pixelCount);
            ammo.TryGetValue(id, out int ammoCount);
            if (pixelCount != ammoCount)
                errors.Add($"Màu {id}: {pixelCount} pixel nhưng tổng ammo = {ammoCount}");
        }

        return result;
    }
}
