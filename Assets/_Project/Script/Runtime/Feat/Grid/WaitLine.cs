using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class WaitLine : MonoBehaviour
{
    [SerializeField] private SelectionEventChannel selectionEventChannel;
    private GridDataSO data;
    private Grid grid;
    private GameObject[] holder;

    private void OnEnable()
    {
        selectionEventChannel.OnEventRaised += GetElement;
    }

    private void OnDisable()
    {
        selectionEventChannel.OnEventRaised -= GetElement;
    }
    

    public void Init(Grid grid,GridDataSO data)
    {
        holder = new GameObject[data.GridSize.x];
        this.data = data;
        this.grid = grid;
        grid.cellSize = data.CellSize;
        grid.cellGap = data.CellGap;
    }

    public void ReArrange(int pos)
    {
        for (int x = pos; x < holder.Length - 1; x++) 
        {
            GameObject next = holder[x + 1];
            holder[x] = next;
        
            if (next != null)
            {
                next.transform.DOMove(grid.GetCellCenterWorld(new Vector3Int(x, 0, 0)), 0.5f);
            }
        }
        holder[holder.Length - 1] = null;
    }
    
    public void Add(GameObject obj)
    {
        if (IsFull()) return;
        for (int x = 0; x < holder.Length; x++)
        {
            if (holder[x] == null)
            {
                holder[x] = obj;
                obj.transform.DOJump(grid.GetCellCenterWorld(new Vector3Int(x, 0, 0)),3,1,0.3f);
                return;
            }
        }
    }

    public void GetElement(Vector3 pos)
    {
      Vector2Int position = (Vector2Int) grid.WorldToCell(pos);
      if(position.y >= data.GridSize.y || position.y < 0) return;
      if(position.x >= data.GridSize.x || position.x < 0) return;
      if(holder[position.x] == null) return;
      Debug.Log(position);
      GameObject obj = holder[position.x];
      holder[position.x] = null;
      Destroy(obj);
      ReArrange(position.x);
    }

    public bool IsFull()
    {
        for (int i = 0; i < holder.Length; i++)
        {
            if (holder[i] == null) return false;
        }
        return true;
    }
    
}
