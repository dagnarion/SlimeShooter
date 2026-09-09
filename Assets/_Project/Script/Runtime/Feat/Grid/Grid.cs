using System.Collections.Generic;
using System;
using UnityEngine;

public class Grid<T>
{
    private Dictionary<Vector3Int, T> grid;
    private Vector3Int gridSize;

    public Grid(Vector3Int gridSize,Func<Vector3Int,T> value)
    {
        this.gridSize = gridSize;
        grid = new Dictionary<Vector3Int, T>();
        for(int x = 0;x<gridSize.x;x++)
        for (int y = 0; y < gridSize.y; y++)
        {
            Vector3Int pos = new Vector3Int(x, y,0);
            grid[pos] = value != null ? value(pos) : default;
        }
    }

    public void GridTraversal(Action<Vector3Int,T> action)
    {
        for(int x = -gridSize.x;x<=gridSize.x;x++)
        for (int y = -gridSize.y; y <= gridSize.y; y++)
        {
            Vector3Int pos = new Vector3Int(x, y , 0);
            grid.TryGetValue(pos, out T val);
            action(pos, val);
        }
    }
    
    public T GetValue(Vector3Int pos)
    {
        if (!IsOnGrid(pos)) return default;
        if (grid.TryGetValue(pos, out T value)) return value;
        return default;
    }


    public void SetValue(Vector3Int pos, T value)
    {
        if (!IsOnGrid(pos)) return;
        grid[pos] = value;
    }


    public bool IsOnGrid(Vector3Int position)
    {
        return Mathf.Abs(position.x) <= gridSize.x && 
               Mathf.Abs(position.y) <= gridSize.y;
    }
}