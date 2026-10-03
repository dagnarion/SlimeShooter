using UnityEngine;

/// <summary>Cấu hình băng chuyền mới (SCN_Gameplay). ConveyorDataSO cũ vẫn dùng cho SCN_UI.</summary>
[CreateAssetMenu(fileName = "SO_ConveyorConfig", menuName = "Conveyor/Conveyor Config")]
public class ConveyorConfigSO : ScriptableObject
{
    [Header("Hình dạng (tính từ mép board)")]
    [Min(0.1f)] [SerializeField] private float margin = 1.2f;
    [Min(0f)] [SerializeField] private float cornerRadius = 0.8f;

    [Header("Chuyển động")]
    [Min(0.1f)] [SerializeField] private float moveSpeed = 5.5f;
    [Tooltip("Khoảng cách tối thiểu giữa 2 shooter trên băng (world unit).")]
    [Min(0.1f)] [SerializeField] private float spacing = 1.3f;
    [Tooltip("Thời gian shooter nhảy từ cột/khay lên cửa băng chuyền.")]
    [Min(0f)] [SerializeField] private float jumpDuration = 0.35f;

    [Header("End rush")]
    [Min(1f)] [SerializeField] private float endRushTimeScale = 1.25f;

    public float Margin => margin;
    public float CornerRadius => cornerRadius;
    public float MoveSpeed => moveSpeed;
    public float Spacing => spacing;
    public float JumpDuration => jumpDuration;
    public float EndRushTimeScale => endRushTimeScale;
}
