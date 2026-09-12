using UnityEngine;


[CreateAssetMenu(menuName = "Grid/GridConfig")]
public class GridDataSO : ScriptableObject
{
    [field: SerializeField] public Vector2Int GridSize;
    [field: SerializeField] public Vector2 CellSize;
    [field: SerializeField] public Vector2 CellGap;
}