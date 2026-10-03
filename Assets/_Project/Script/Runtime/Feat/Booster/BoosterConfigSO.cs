using System;
using System.Collections.Generic;
using UnityEngine;

public enum BoosterType
{
    /// <summary>+1 slot băng chuyền trong level hiện tại.</summary>
    AddSlot,
    /// <summary>Chọn bất kỳ shooter nào trong cột (không cần đứng đầu).</summary>
    Pickup,
    /// <summary>Chọn một màu: phá mọi ô màu đó và bỏ mọi shooter màu đó.</summary>
    ColorBomb,
    /// <summary>Xáo shooter giữa các cột.</summary>
    Shuffle
}

[Serializable]
public struct BoosterEntry
{
    public BoosterType type;
    public string displayName;
    [Min(0)] public int startCount;
    [Min(1)] public int unlockLevel;
}

[CreateAssetMenu(fileName = "SO_BoosterConfig", menuName = "Booster/Booster Config")]
public class BoosterConfigSO : ScriptableObject
{
    [SerializeField] private List<BoosterEntry> boosters = new List<BoosterEntry>
    {
        new BoosterEntry { type = BoosterType.AddSlot, displayName = "+SLOT", startCount = 2, unlockLevel = 1 },
        new BoosterEntry { type = BoosterType.Pickup, displayName = "PICK", startCount = 2, unlockLevel = 1 },
        new BoosterEntry { type = BoosterType.ColorBomb, displayName = "BOMB", startCount = 2, unlockLevel = 1 },
        new BoosterEntry { type = BoosterType.Shuffle, displayName = "SHUFFLE", startCount = 2, unlockLevel = 1 },
    };

    public IReadOnlyList<BoosterEntry> Boosters => boosters;

    public BoosterEntry Get(BoosterType type)
    {
        foreach (var entry in boosters) if (entry.type == type) return entry;
        return new BoosterEntry { type = type, displayName = type.ToString(), startCount = 0, unlockLevel = 1 };
    }
}
