using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "SO_LevelDatabase", menuName = "Level/Level Database")]
public class LevelDatabaseSO : ScriptableObject
{
    [SerializeField] private List<LevelSO> levels = new List<LevelSO>();

    [Tooltip("Chơi hết level cuối thì quay lại level có index này.")]
    [Min(0)] [SerializeField] private int loopStartIndex;

    public int Count => levels.Count;
    public int LoopStartIndex => Mathf.Clamp(loopStartIndex, 0, Mathf.Max(0, levels.Count - 1));
    public IReadOnlyList<LevelSO> Levels => levels;

    public LevelSO Get(int index)
    {
        if (levels.Count == 0) return null;
        return levels[Mathf.Clamp(index, 0, levels.Count - 1)];
    }

    public void SetLevels(IEnumerable<LevelSO> newLevels)
    {
        levels = new List<LevelSO>(newLevels);
    }
}
