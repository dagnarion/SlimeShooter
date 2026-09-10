using System;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;

public class GridController : MonoBehaviour
{
    [SerializeField] private GridDataSO data;
    [SerializeField] private GridRender render;
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Grid gridComponent;
    [SerializeField] private Transform holder;
    [SerializeField] private SelectionEventChannel selectionEventChannel;
    private Grid<GameObject> grid;

    private void OnEnable()
    {
        selectionEventChannel.OnEventRaised += Choose;
    }

    private void OnDisable()
    {
        selectionEventChannel.OnEventRaised -= Choose;
    }

    private void Start()
    {
        Init();
    }

    [Button]
    private void Init()
    {
        holder.Clear();
        render.Init(gridComponent,data);
        grid = new Grid<GameObject>(data.GridSize, Pos =>
        {
            GameObject slime = Instantiate(slimePrefab,gridComponent.GetCellCenterWorld(new Vector3Int(Pos.x,Pos.y,0)),Quaternion.identity,holder);
            return slime;
        });
    }

    private void Choose(Vector3 pos)
    {
       if(!TryGetSlime(pos)) return;
       Vector2Int position = (Vector2Int)gridComponent.WorldToCell(pos);
       GameObject gameObject = grid.GetValue(position);
       Destroy(gameObject);
       Rearrange(position);
    }

    private void Rearrange(Vector2Int pos)
    {
        for (int y = pos.y; y >= 0; y--)
        {
            GameObject nextValue = grid.GetValue(new Vector2Int(pos.x, y - 1));
            grid.SetValue(new Vector2Int(pos.x,y),nextValue);
            if (nextValue != null)
            {
                nextValue.transform.position = gridComponent.GetCellCenterWorld(new Vector3Int(pos.x, y, 0));
            }
        }
    }
    
    private bool TryGetSlime(Vector3 pos)
    {
        Vector2Int position = (Vector2Int)gridComponent.WorldToCell(pos);
        if(!grid.IsOnGrid(position)) return false;
        if(grid.GetValue(position+new Vector2Int(0,1)) != null) return false;
        return true;
    }
        
    
}
