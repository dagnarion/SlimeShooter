using System;
using UnityEngine;

public class Grid<T>
{
    private T[,] grid;
    private Vector2Int size;

    public Grid(Vector2Int size, Func<Vector2Int, T> factory)
    {
        this.size = size;
        grid = new T[size.x, size.y];
        
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            grid[x, y] = factory(new Vector2Int(x, y));
        }
    }

    public void GridTraversal(Action<T> action)
    {
        for (int x = 0; x < size.x; x++)
        for (int y = 0; y < size.y; y++)
        {
            action?.Invoke(grid[x, y]);
        }
    }

    public T GetElement(Vector2Int pos)
    {
        if (!IsOnGrid(pos))
        {
            Debug.LogWarning($"{pos} out of the range");
            return default;
        }

        return grid[pos.x, pos.y];
    }

    public void SetElement(Vector2Int pos, T value)
    {
        if (!IsOnGrid(pos))
        {
            Debug.LogWarning($"{value} out of the range");
            return;
        }

        grid[pos.x, pos.y] = value;
    }

    public bool IsOnGrid(Vector2Int pos)
    {
        if (pos.x < 0 || pos.x >= size.x || pos.y < 0 || pos.y >= size.y) return false;
        return true;
    }
}