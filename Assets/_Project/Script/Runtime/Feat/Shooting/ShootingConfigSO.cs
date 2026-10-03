using UnityEngine;

[CreateAssetMenu(fileName = "SO_ShootingConfig", menuName = "Shooting/Shooting Config")]
public class ShootingConfigSO : ScriptableObject
{
    [Tooltip("Bước lấy mẫu quãng đường khi dò đường bắn, tính theo tỉ lệ cellSize (nhỏ hơn 0.5 để không bỏ cột).")]
    [Range(0.05f, 0.5f)] [SerializeField] private float sampleStepRatio = 0.25f;

    [Header("End rush")]
    [Tooltip("Khoảng cách giữa 2 phát bắn khi rush (giây). Game gốc ≈ 0.05.")]
    [Min(0.005f)] [SerializeField] private float rushFireInterval = 0.05f;
    [Tooltip("Rush kéo dài quá thời gian này mà board chưa sạch thì cho vỡ hết ô còn lại.")]
    [Min(1f)] [SerializeField] private float rushTimeout = 10f;

    public float SampleStepRatio => sampleStepRatio;
    public float RushFireInterval => rushFireInterval;
    public float RushTimeout => rushTimeout;
}
