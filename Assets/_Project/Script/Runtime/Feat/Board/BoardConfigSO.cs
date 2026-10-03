using UnityEngine;

[CreateAssetMenu(fileName = "SO_BoardConfig", menuName = "Board/Board Config")]
public class BoardConfigSO : ScriptableObject
{
    [Min(0.01f)] [SerializeField] private float cellSize = 1f;

    [Tooltip("Thời gian đạn bay từ shooter tới ô (giây).")]
    [Min(0f)] [SerializeField] private float bulletTravelTime = 0.15f;

    [Tooltip("Thời gian animation vỡ ô (giây).")]
    [Min(0f)] [SerializeField] private float breakAnimDuration = 0.2f;

    public float CellSize => cellSize;
    public float BulletTravelTime => bulletTravelTime;
    public float BreakAnimDuration => breakAnimDuration;

    /// <summary>Thời gian một ô ở trạng thái Breaking (chặn ô phía sau) trước khi Dead.</summary>
    public float BreakTime => bulletTravelTime + breakAnimDuration;
}
