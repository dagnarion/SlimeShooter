using System.Collections.Generic;
using System;
using UnityEngine;

public class Grid<T>
{
    private Dictionary<Vector2Int, T> grid;
    private Vector2Int gridSize;

    public Grid(Vector2Int gridSize,Func<Vector2Int,T> value)
    {
        this.gridSize = gridSize;
        grid = new Dictionary<Vector2Int, T>();
        for(int x = 0;x<gridSize.x;x++)
        for (int y = 0; y < gridSize.y; y++)
        {
            Vector2Int pos = new Vector2Int(x, y);
            grid[pos] = value != null ? value(pos) : default;
        }
    }

    public void GridTraversal(Action<Vector2Int,T> action)
    {
        for(int x = 0;x<gridSize.x;x++)
        for (int y = 0; y < gridSize.y; y++)
        {
            Vector2Int pos = new Vector2Int(x, y);
            grid.TryGetValue(pos, out T val);
            action(pos, val);
        }
    }
    
    public T GetValue(Vector2Int pos)
    {
        if (!IsOnGrid(pos)) return default;
        if (grid.TryGetValue(pos, out T value)) return value;
        return default;
    }


    public void SetValue(Vector2Int pos, T value)
    {
        if (!IsOnGrid(pos)) return;
        grid[pos] = value;
    }


    public bool IsOnGrid(Vector2Int position)
    {
        return Mathf.Abs(position.x) <= gridSize.x && 
               Mathf.Abs(position.y) <= gridSize.y;
    }
}