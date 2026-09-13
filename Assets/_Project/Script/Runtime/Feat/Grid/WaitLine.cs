using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class WaitLine : MonoBehaviour
{
    [SerializeField] private SelectionEventChannel selectionEventChannel;
    [SerializeField] private MovableEventChanelSO onItemExited;
    [SerializeField] private VoidEventChanelSO onGameOverEvent;
    [SerializeField] private MovableEventChanelSO onAddMovable;
    
    public bool IsGameOver { get; private set; }

    private GridDataSO data;
    private Grid grid;
    private IGamePices[] holder;

    private void OnEnable()
    {
        if (selectionEventChannel != null)
        {
            selectionEventChannel.OnEventRaised += GetElement;
        }

        if (onItemExited != null)
        {
            onItemExited.OnEventRaised += HandleItemExited;
        }
    }

    private void OnDisable()
    {
        if (selectionEventChannel != null)
        {
            selectionEventChannel.OnEventRaised -= GetElement;
        }

        if (onItemExited != null)
        {
            onItemExited.OnEventRaised -= HandleItemExited;
        }
    }
    

    public void Init(Grid grid,GridDataSO data)
    {
        holder = new IGamePices[data.GridSize.x];
        this.data = data;
        this.grid = grid;
        grid.cellSize = data.CellSize;
        grid.cellGap = data.CellGap;
    }

    public void ReArrange(int pos)
    {
        for (int x = pos; x < holder.Length - 1; x++) 
        {
            IGamePices next = holder[x + 1];
            holder[x] = next;
        
            if (next != null)
            {
                next.Transform.DOMove(grid.GetCellCenterWorld(new Vector3Int(x, 0, 0)), 0.5f);
            }
        }
        holder[holder.Length - 1] = null;
    }
    
    private void HandleItemExited(IGamePices movable)
    {
        if (movable == null || movable == null) return;
        
        if (movable.Movement is IDisposable disposable)
        {
            disposable.Dispose();
        }

        Add(movable);
    }
    
    public bool Add(IGamePices obj)
    {
        if (IsGameOver || obj == null) return false;
        
        if (IsFull())
        {
            TriggerGameOver();
            return false;
        }

        if (holder == null || grid == null) return false;
        
        obj.Movement.DetachFromBelt(); // test
        
        for (int x = 0; x < holder.Length; x++)
        {
            if (holder[x] == null)
            {
                holder[x] = obj;
                Vector3 targetPos = grid.GetCellCenterWorld(new Vector3Int(x, 0, 0));
                
                obj.Transform.DOKill();
                obj.Transform.DOJump(targetPos, jumpPower: 2f, numJumps: 1, duration: 0.4f)
                    .SetEase(Ease.OutQuad);
                obj.Transform.DORotate(Vector3.zero, 0.4f);
                return true;
            }
        }

        TriggerGameOver();
        return false;
    }

    public void GetElement(Vector3 pos)
    {
        if (IsGameOver || grid == null || data == null || holder == null) return;
        Vector2Int position = (Vector2Int) grid.WorldToCell(pos);
        if (position.y >= data.GridSize.y || position.y < 0) return;
        if (position.x >= data.GridSize.x || position.x < 0) return;
        if (holder[position.x] == null) return;

        IGamePices obj = holder[position.x];

        if (onAddMovable != null)
        {
            obj.Transform.DOKill();
            bool accepted = false;
            IGamePices movable = obj;
            movable.Movement.OnAccepted = () =>
            {
                accepted = true;
                holder[position.x] = null;
                ReArrange(position.x);
            };

            onAddMovable.EventRaise(movable);

            if (!accepted)
            {
                obj.Transform.position = grid.GetCellCenterWorld(new Vector3Int(position.x, 0, 0));
            }
        }
        // else
        // {
        //     holder[position.x] = null;
        //     // Destroy(obj);
        //     ReArrange(position.x);
        // }
    }

    public bool IsFull()
    {
        if (holder == null) return false;
        for (int i = 0; i < holder.Length; i++)
        {
            if (holder[i] == null) return false;
        }
        return true;
    }

    private void TriggerGameOver()
    {
        if (IsGameOver) return;
        IsGameOver = true;
        Debug.Log("<color=red><b>[GAME OVER] WaitLine is full! You Lose!</b></color>");
        onGameOverEvent?.EventRaise();
    }
    
}
