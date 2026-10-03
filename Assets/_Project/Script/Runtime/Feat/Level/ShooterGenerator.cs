using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Sinh shooter từ số pixel mỗi màu sao cho Σammo(màu) == Σpixel(màu),
/// rồi xáo theo seed và chia đều vào các cột.
/// </summary>
public static class ShooterGenerator
{
    public struct Settings
    {
        public int ColumnCount;
        /// <summary>Các mức ammo cho phép, ví dụ {10, 20, 30}. Phần lẻ nhỏ hơn mức nhỏ nhất được cộng vào shooter cuối của màu đó.</summary>
        public int[] AmmoSteps;
        public int Seed;
        /// <summary>
        /// > 0: bỏ qua AmmoSteps, tự tính bậc ammo sao cho tổng số shooter xấp xỉ giá trị này
        /// (nhiều shooter hơn số slot băng chuyền = level khó hơn, end rush tới muộn hơn).
        /// </summary>
        public int TargetShooterCount;
    }

    /// <summary>Bậc ammo quanh mức trung bình để đạt khoảng <paramref name="target"/> shooter.</summary>
    public static int[] StepsForTarget(int totalPixels, int target)
    {
        int average = Math.Max(1, (int)Math.Round(totalPixels / (double)Math.Max(1, target)));
        return new[] { Math.Max(1, (int)Math.Round(average * 0.6)), average, Math.Max(1, (int)Math.Round(average * 1.4)) }
            .Distinct().OrderBy(s => s).ToArray();
    }

    public static List<ColumnSpec> Generate(IReadOnlyDictionary<int, int> pixelsPerColor, Settings settings)
    {
        var requested = settings.TargetShooterCount > 0
            ? StepsForTarget(pixelsPerColor.Values.Sum(), settings.TargetShooterCount)
            : settings.AmmoSteps;
        var steps = (requested ?? Array.Empty<int>()).Where(s => s > 0).Distinct().OrderBy(s => s).ToArray();
        if (steps.Length == 0) throw new ArgumentException("AmmoSteps phải có ít nhất một giá trị > 0");

        var random = new Random(settings.Seed);
        var shooters = new List<ShooterSpec>();

        foreach (int colorId in pixelsPerColor.Keys.OrderBy(k => k))
        {
            SplitColor(colorId, pixelsPerColor[colorId], steps, random, shooters);
        }

        Shuffle(shooters, random);

        int columnCount = Math.Max(1, Math.Min(settings.ColumnCount, shooters.Count));
        var columns = new List<ColumnSpec>(columnCount);
        for (int i = 0; i < columnCount; i++) columns.Add(new ColumnSpec());
        for (int i = 0; i < shooters.Count; i++) columns[i % columnCount].shooters.Add(shooters[i]);

        return columns;
    }

    private static void SplitColor(int colorId, int pixelCount, int[] steps, Random random, List<ShooterSpec> output)
    {
        int remaining = pixelCount;
        int lastIndex = -1;

        while (remaining > 0)
        {
            int candidateCount = 0;
            while (candidateCount < steps.Length && steps[candidateCount] <= remaining) candidateCount++;

            if (candidateCount == 0)
            {
                // Phần lẻ nhỏ hơn mức ammo nhỏ nhất.
                if (lastIndex >= 0)
                {
                    var last = output[lastIndex];
                    output[lastIndex] = new ShooterSpec(colorId, last.ammo + remaining);
                }
                else
                {
                    output.Add(new ShooterSpec(colorId, remaining));
                }
                return;
            }

            int ammo = steps[random.Next(candidateCount)];
            output.Add(new ShooterSpec(colorId, ammo));
            lastIndex = output.Count - 1;
            remaining -= ammo;
        }
    }

    private static void Shuffle<T>(IList<T> list, Random random)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);
            (list[i], list[j]) = (list[j], list[i]);
        }
    }
}
