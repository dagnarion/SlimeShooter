using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;

public class GridController : MonoBehaviour
{
    [SerializeField] private GridDataSO gridData;
    [SerializeField] private GridDataSO waitLinedata;
    [SerializeField] private GridRender gridRender;
    [SerializeField] private GridRender waitLineRender;
    [SerializeField] private Grid gridComponent;
    [SerializeField] private Grid waitLineGridComponent;
    [SerializeField] private WaitLine waitLine;
    
    [SerializeField] private Transform holder;
    [SerializeField] private GameObject slimePrefab;
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
        gridRender.Init(gridComponent,gridData);
        waitLineRender.Init(waitLineGridComponent,waitLinedata);
        grid = new Grid<GameObject>(gridData.GridSize, Pos =>
        {
            GameObject slime = Instantiate(slimePrefab,gridComponent.GetCellCenterWorld(new Vector3Int(Pos.x,Pos.y,0)),Quaternion.identity,holder);
            return slime;
        });
        waitLine.Init(waitLineGridComponent,waitLinedata);
    }

    private void Choose(Vector3 pos)
    {
       if(!TryGetSlime(pos)) return;
       Vector2Int position = (Vector2Int)gridComponent.WorldToCell(pos);
       GameObject gameObject = grid.GetValue(position);
       // test
       if(waitLine.IsFull()) return;
       waitLine.Add(gameObject);
       //Destroy(gameObject);
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
                nextValue.transform.DOMove( gridComponent.GetCellCenterWorld(new Vector3Int(pos.x, y, 0)),0.5f);
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
