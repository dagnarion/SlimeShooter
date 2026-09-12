using UnityEngine;

[CreateAssetMenu(fileName = "SO_ConveyorData", menuName = "Conveyor/ConveyorDataSO")]
public class ConveyorDataSO : ScriptableObject
{
    [Range(0f, 1f)]
    [SerializeField] private float startPointInConveyor;
    
    [Range(0f, 1f)]
    [SerializeField] private float endPointInConveyor;
    
    [Min(0f)]
    [SerializeField] private float moveSpeed;
    [SerializeField] private float jumpDuration;
    [SerializeField] private float dropDuration;
    
    [SerializeField] private int maxBeltCapacity;
    [SerializeField] private int maxQueueCapacity;
    
    [SerializeField] private float safeDistance;
    [SerializeField] private float itemHeight;

    
    public float StartPointInConveyor => startPointInConveyor;
    public float EndPointInConveyor => endPointInConveyor;
    public float MoveSpeed => moveSpeed;
    public float JumpDuration => jumpDuration;
    public float DropDuration => dropDuration;
    public  int MaxBeltCapacity => maxBeltCapacity;
    public int MaxQueueCapacity => maxQueueCapacity;
    public int MaxCapacity => maxBeltCapacity +  maxQueueCapacity;
    public  float SafeDistance => safeDistance;
    public float ItemHeight => itemHeight;
}
