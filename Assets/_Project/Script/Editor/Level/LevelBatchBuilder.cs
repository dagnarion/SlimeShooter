using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Sinh hàng loạt level từ một thư mục ảnh PNG (sắp theo tên) và chấm thử từng level bằng bot (GameplaySim).
/// </summary>
public static class LevelBatchBuilder
{
    public struct Report
    {
        public string LevelName;
        public string Source;
        public bool Valid;
        public int Shooters;
        public int Colors;
        public int Pixels;
        public GameState BotResult;
        public float BotTime;
        public float RushAt;
        public string Note;

        public override string ToString() =>
            $"{LevelName} ← {Source}: {(Valid ? "OK" : "INVALID")} | {Pixels} px, {Colors} màu, {Shooters} shooter | " +
            $"bot {BotResult} {BotTime:0.0}s{(RushAt >= 0 ? $", rush lúc {RushAt:0.0}s" : ", không rush")}{(string.IsNullOrEmpty(Note) ? "" : " | " + Note)}";
    }

    public static List<Report> Build(string sourceFolder, string outputFolder, LevelAssetBuilder.Options options,
        int startNumber, LevelDatabaseSO database, bool replaceDatabase, bool simulate)
    {
        var reports = new List<Report>();
        var textures = AssetDatabase.FindAssets("t:Texture2D", new[] { sourceFolder })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(p => Path.GetDirectoryName(p)?.Replace('\\', '/') == sourceFolder.TrimEnd('/'))
            .OrderBy(p => p, System.StringComparer.OrdinalIgnoreCase)
            .ToList();

        var levels = new List<LevelSO>();
        for (int i = 0; i < textures.Count; i++)
        {
            var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(textures[i]);
            EditorUtility.DisplayProgressBar("Level batch", texture.name, i / (float)textures.Count);

            var perLevel = options;
            perLevel.Seed = options.Seed + i;
            string path = $"{outputFolder.TrimEnd('/')}/Level_{startNumber + i:000}.asset";
            var level = LevelAssetBuilder.CreateOrUpdate(path, texture, perLevel, out var preview);
            levels.Add(level);

            var report = new Report
            {
                LevelName = level.name,
                Source = texture.name,
                Valid = preview.Validation.IsValid,
                Shooters = preview.Columns.Sum(c => c.shooters.Count),
                Colors = LevelBaker.CountPerColor(preview.Bake.Cells).Count,
                Pixels = LevelBaker.CountPerColor(preview.Bake.Cells).Values.Sum(),
                RushAt = -1f,
                Note = preview.Validation.IsValid ? "" : preview.Validation.ToString()
            };
            if (simulate && report.Valid) Simulate(level, ref report);
            reports.Add(report);
        }
        EditorUtility.ClearProgressBar();

        if (database != null)
        {
            var list = replaceDatabase ? new List<LevelSO>() : database.Levels.ToList();
            foreach (var level in levels) if (!list.Contains(level)) list.Add(level);
            database.SetLevels(list);
            EditorUtility.SetDirty(database);
        }
        AssetDatabase.SaveAssets();
        return reports;
    }

    public static void Simulate(LevelSO level, ref Report report)
    {
        using (var sim = GameplaySim.FromLevel(level))
        {
            float rushAt = -1f;
            float nextDecision = 0f;
            while (sim.Session.IsPlaying && sim.Time < 600f)
            {
                if (sim.Time >= nextDecision) { sim.BotPick(); nextDecision = sim.Time + 0.15f; }
                sim.Step(1f / 60f);
                if (rushAt < 0f && sim.EndRush.IsActive.CurrentValue) rushAt = sim.Time;
            }
            report.BotResult = sim.Session.CurrentState;
            report.BotTime = sim.Time;
            report.RushAt = rushAt;
        }
    }
}
