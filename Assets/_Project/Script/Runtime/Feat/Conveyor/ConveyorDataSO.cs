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
    
    public float StartPointInConveyor => startPointInConveyor;
    public float EndPointInConveyor => endPointInConveyor;
    public float MoveSpeed => moveSpeed;
}
