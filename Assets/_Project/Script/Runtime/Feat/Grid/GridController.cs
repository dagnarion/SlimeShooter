using System;
using DG.Tweening;
using NaughtyAttributes;
using UnityEngine;
using UnityEngine.Rendering;
using Random = UnityEngine.Random;

public class GridController : MonoBehaviour
{
    #region Grid
    [SerializeField] private GridDataSO gridData;
    [SerializeField] private GridDataSO waitLinedata;
    
    [SerializeField] private GridRender gridRender;
    [SerializeField] private GridRender waitLineRender;
    
    [SerializeField] private Grid gridComponent;
    [SerializeField] private Grid waitLineGridComponent;
    [SerializeField] private WaitLine waitLine;
    private Grid<IGamePieces> grid;
    #endregion

    #region Slime
    [SerializeField] private SlimeDataSO[] slimeDataSos; // test
    [SerializeField] private Transform holder;
    [SerializeField] private ComponentPoolSO<SlimeController> slimePool;
 //   [SerializeField] private SlimeController slimePrefab; // tách ra thành một factory spawn game pices
    #endregion

    #region Event
    [SerializeField] private SelectionEventChannel selectionEventChannel;
    [SerializeField] private MovableEventChanelSO onAddMovable;
    #endregion
    
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
        slimePool.InitPool(holder); // để tạm, sau khởi tạo trong boostrap
        grid = new Grid<IGamePieces>(gridData.GridSize, Pos =>
        {
            SlimeController slime = slimePool.Get();
            slime.transform.position = gridComponent.GetCellCenterWorld(new Vector3Int(Pos.x, Pos.y, 0));
            slime.transform.rotation = Quaternion.identity;
            slime.Init(slimeDataSos[Random.Range(0,slimeDataSos.Length)]);
            return slime as IGamePieces;
        });
        waitLine.Init(waitLineGridComponent,waitLinedata);
    }

    private void Choose(Vector3 pos)
    {
       if (waitLine != null && waitLine.IsGameOver) return;
       if (!TryGetSlime(pos)) return;
       Vector2Int position = (Vector2Int)gridComponent.WorldToCell(pos);
       
       IGamePieces slime = grid.GetValue(position);
       if (slime == null) return;
       
       IGamePieces movable = slime;
       movable.Movement.OnAccepted = () =>
       {
           grid.SetValue(position, null);
           Rearrange(position);
       };
       
       onAddMovable?.EventRaise(movable);
    }

    private void Rearrange(Vector2Int pos)
    {
        for (int y = pos.y; y >= 0; y--)
        {
            IGamePieces nextValue = grid.GetValue(new Vector2Int(pos.x, y - 1));
            grid.SetValue(new Vector2Int(pos.x,y),nextValue);
            if (nextValue != null)
            {
                nextValue.Transform.DOMove( gridComponent.GetCellCenterWorld(new Vector3Int(pos.x, y, 0)),0.5f);
            }
        }
    }
    
    private bool TryGetSlime(Vector3 pos)
    {
        if (grid == null) return false;
        Vector2Int position = (Vector2Int)gridComponent.WorldToCell(pos);
        if(!grid.IsOnGrid(position)) return false;
        if(grid.GetValue(position) == null) return false;
        if(grid.GetValue(position+new Vector2Int(0,1)) != null) return false;
        return true;
    }
        
    
}
