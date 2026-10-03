using NUnit.Framework;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Chơi tự động các level thật trong SO_LevelDatabase bằng model (không scene).
/// Kiểm tra core loop chạy trọn: chọn -> băng -> bắn -> khay -> rush -> thắng.
/// </summary>
public class AutoplayTests
{
    private const string DatabasePath = "Assets/_Project/Resource/SO/Levels/SO_LevelDatabase.asset";

    private static LevelDatabaseSO LoadDatabase()
    {
        var database = AssetDatabase.LoadAssetAtPath<LevelDatabaseSO>(DatabasePath);
        Assert.IsNotNull(database, DatabasePath);
        return database;
    }

    [Test]
    public void AllLevels_AreWinnableByBot([Values(0, 1, 2, 3, 4)] int index)
    {
        var level = LoadDatabase().Get(index);
        Assert.IsTrue(LevelValidator.Validate(level).IsValid, level.name);

        using (var sim = GameplaySim.FromLevel(level))
        {
            var result = sim.Autoplay();
            Debug.Log($"[Autoplay] {level.name}: {result} sau {sim.Time:0.0}s, rush={sim.EndRush.IsActive.CurrentValue}, " +
                      $"còn {sim.Board.Remaining.CurrentValue} ô, khay {sim.Tray.Count}/{sim.Tray.Capacity.CurrentValue}");
            Assert.AreEqual(GameState.Won, result, $"{level.name}: {sim.Rules.LoseReason}");
        }
    }
}
