using System;
using UnityEngine;

public class GridRender : MonoBehaviour
{
   private Grid grid;
   private GridDataSO config;

   public void Init(Grid grid,GridDataSO config)
   {
      this.grid = grid;
      this.config = config;
   }
   
   private void CenterGrid()
   {
      Vector3 firstCell = grid.GetCellCenterWorld(Vector3Int.zero);
      Vector3 lastCell = grid.GetCellCenterWorld(new Vector3Int(config.GridSize.x - 1, config.GridSize.y - 1, 0));
      Vector3 gridCenter = (firstCell + lastCell) * 0.5f;
      gridCenter.z = 0;
      gridCenter.y = 0;
      grid.transform.position -= gridCenter;
   }

   private void DrawGrid()
   {
      if (grid == null || config == null) return;
      CenterGrid();
      grid.cellGap = config.CellGap;
      grid.cellSize = new Vector3(config.CellSize.x, config.CellSize.y, 0);
      Gizmos.color = Color.red;
      for (int x = 0; x < config.GridSize.x; x++)
      {
         for (int y = 0; y < config.GridSize.y; y++)
         {
            Vector3Int cell = new Vector3Int(x, y, 0);
            Vector3 position = grid.GetCellCenterWorld(cell);
            Vector3 cellSize = new Vector3(grid.cellSize.x, 0, grid.cellSize.y);
            Gizmos.DrawWireCube(position, cellSize);
         }
      }
   }

   private void OnDrawGizmos()
   {
      DrawGrid();
   }
}
