using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using static LevelTestUtils;

public class PaletteTests
{
    [Test]
    public void FindNearest_MapsSlightlyOffColor_ToClosestEntry()
    {
        var palette = CreatePalette();

        Assert.IsTrue(palette.TryFindNearest(new Color32(220, 50, 45, 255), out int id, out _));
        Assert.AreEqual(Red, id);

        palette.TryFindNearest(new Color32(50, 90, 210, 255), out id, out _);
        Assert.AreEqual(Blue, id);
    }

    [Test]
    public void GetColor_UnknownId_ReturnsMagenta()
    {
        var palette = CreatePalette();
        Assert.AreEqual(new Color32(255, 0, 255, 255), palette.GetColor(99));
        Assert.IsFalse(palette.Contains(99));
    }
}

public class LevelBakerTests
{
    [Test]
    public void Bake_TransparentBecomesEmpty_AndColorsMapToIds()
    {
        var palette = CreatePalette();
        var clear = new Color32(0, 0, 0, 0);
        var red = new Color32(230, 40, 40, 255);
        var green = new Color32(40, 200, 60, 255);
        // 2x2, hàng dưới trước: (0,0)=red (1,0)=clear (0,1)=green (1,1)=red
        var pixels = new[] { red, clear, green, red };

        var result = LevelBaker.Bake(pixels, 2, 2, palette);

        CollectionAssert.AreEqual(new[] { Red, LevelSO.EmptyCell, Green, Red }, result.Cells);
        Assert.AreEqual(0f, result.MaxColorDistance, 0.001f);
    }

    [Test]
    public void CountPerColor_IgnoresEmpty()
    {
        var counts = LevelBaker.CountPerColor(new[] { Red, Red, LevelSO.EmptyCell, Blue });
        Assert.AreEqual(2, counts[Red]);
        Assert.AreEqual(1, counts[Blue]);
        Assert.IsFalse(counts.ContainsKey(LevelSO.EmptyCell));
    }
}

public class ShooterGeneratorTests
{
    private static readonly Dictionary<int, int> Pixels = new Dictionary<int, int> { { Red, 57 }, { Green, 30 }, { Blue, 8 } };

    private static ShooterGenerator.Settings Settings(int seed = 7, int columns = 3) => new ShooterGenerator.Settings
    {
        ColumnCount = columns,
        AmmoSteps = new[] { 10, 20 },
        Seed = seed
    };

    [Test]
    public void TotalAmmoPerColor_EqualsPixelCount()
    {
        var columns = ShooterGenerator.Generate(Pixels, Settings());
        var ammo = columns.SelectMany(c => c.shooters).GroupBy(s => s.colorId).ToDictionary(g => g.Key, g => g.Sum(s => s.ammo));

        Assert.AreEqual(57, ammo[Red]);
        Assert.AreEqual(30, ammo[Green]);
        Assert.AreEqual(8, ammo[Blue]); // nhỏ hơn bậc nhỏ nhất -> 1 shooter 8 ammo
    }

    [Test]
    public void SameSeed_SameResult_DifferentSeed_DifferentOrder()
    {
        string Serialize(List<ColumnSpec> cols) => string.Join("|", cols.Select(c => string.Join(",", c.shooters.Select(s => $"{s.colorId}:{s.ammo}"))));

        Assert.AreEqual(Serialize(ShooterGenerator.Generate(Pixels, Settings(1))), Serialize(ShooterGenerator.Generate(Pixels, Settings(1))));

        var distinct = Enumerable.Range(1, 5).Select(seed => Serialize(ShooterGenerator.Generate(Pixels, Settings(seed)))).Distinct().Count();
        Assert.Greater(distinct, 1);
    }

    [Test]
    public void ColumnsAreBalanced_AndNeverEmpty()
    {
        var columns = ShooterGenerator.Generate(Pixels, Settings(columns: 4));
        var sizes = columns.Select(c => c.shooters.Count).ToList();

        Assert.IsTrue(sizes.All(s => s > 0));
        Assert.LessOrEqual(sizes.Max() - sizes.Min(), 1);
    }

    [Test]
    public void MoreColumnsThanShooters_ClampsColumnCount()
    {
        var columns = ShooterGenerator.Generate(new Dictionary<int, int> { { Red, 10 } }, Settings(columns: 5));
        Assert.AreEqual(1, columns.Count);
    }

    [Test]
    public void AmmoUsesConfiguredSteps_ExceptRemainderMerge()
    {
        var columns = ShooterGenerator.Generate(Pixels, Settings());
        // Mọi ammo phải là 10, 20 hoặc bậc + phần lẻ (<10) hoặc phần lẻ đứng riêng.
        foreach (var spec in columns.SelectMany(c => c.shooters))
            Assert.IsTrue(spec.ammo > 0 && spec.ammo < 30, $"ammo {spec.ammo} ngoài khoảng kỳ vọng");
    }
}

public class LevelValidatorTests
{
    private static readonly int[] Cells = { Red, Red, Green, LevelSO.EmptyCell }; // 2x2: Red=2, Green=1

    [Test]
    public void Valid_WhenAmmoMatchesPixels()
    {
        var columns = new List<ColumnSpec> { Column((Red, 2)), Column((Green, 1)) };
        var result = LevelValidator.Validate(2, 2, Cells, columns, CreatePalette(), 5, 5);
        Assert.IsTrue(result.IsValid, result.ToString());
    }

    [Test]
    public void ReportsMismatchedColor()
    {
        var columns = new List<ColumnSpec> { Column((Red, 3)), Column((Green, 1)) };
        var result = LevelValidator.Validate(2, 2, Cells, columns, CreatePalette(), 5, 5);

        Assert.IsFalse(result.IsValid);
        Assert.AreEqual(1, result.Errors.Count, result.ToString());
        StringAssert.Contains($"Màu {Red}", result.Errors[0]);
    }

    [Test]
    public void ReportsUnknownColor_EmptyColumn_AndBadSize()
    {
        var columns = new List<ColumnSpec> { Column((Red, 2), (Green, 1)), new ColumnSpec(), Column((99, 1)) };
        var result = LevelValidator.Validate(2, 2, Cells, columns, CreatePalette(), 0, 5);

        Assert.IsTrue(result.Errors.Any(e => e.Contains("Cột 1 rỗng")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("Màu 99 của shooter không có trong palette")));
        Assert.IsTrue(result.Errors.Any(e => e.Contains("conveyorSlots")));
    }

    [Test]
    public void ReportsCellCountMismatch()
    {
        var result = LevelValidator.Validate(3, 3, Cells, new List<ColumnSpec> { Column((Red, 2)) }, null, 5, 5);
        Assert.IsFalse(result.IsValid);
    }
}

public class LevelServiceTests
{
    private static LevelDatabaseSO CreateDatabase(int count)
    {
        var db = ScriptableObject.CreateInstance<LevelDatabaseSO>();
        db.SetLevels(Enumerable.Range(0, count).Select(_ => ScriptableObject.CreateInstance<LevelSO>()));
        return db;
    }

    [Test]
    public void LoadsSavedIndex_AndAdvanceSaves()
    {
        var save = new FakeSave();
        save.SetInt(LevelService.CurrentLevelKey, 1);
        var db = CreateDatabase(3);

        using (var service = new LevelService(db, save))
        {
            Assert.AreEqual(1, service.CurrentIndex.CurrentValue);
            Assert.AreSame(db.Get(1), service.Current);

            service.Advance();

            Assert.AreEqual(2, service.CurrentIndex.CurrentValue);
            Assert.AreEqual(2, save.GetInt(LevelService.CurrentLevelKey));
            Assert.AreEqual(1, save.SaveCalls);
        }
    }

    [Test]
    public void Advance_PastLast_LoopsToLoopStart()
    {
        var save = new FakeSave();
        save.SetInt(LevelService.CurrentLevelKey, 2);

        using (var service = new LevelService(CreateDatabase(3), save))
        {
            service.Advance();
            Assert.AreEqual(0, service.CurrentIndex.CurrentValue);
        }
    }

    [Test]
    public void SavedIndexOutOfRange_IsClamped()
    {
        var save = new FakeSave();
        save.SetInt(LevelService.CurrentLevelKey, 42);

        using (var service = new LevelService(CreateDatabase(2), save))
        {
            Assert.AreEqual(0, service.CurrentIndex.CurrentValue);
        }
    }
}
