using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Dữ liệu một level. Lưới pixel lưu dạng row-major, y = 0 là hàng dưới cùng
/// (cùng thứ tự với Texture2D.GetPixels32). Ô trống = <see cref="EmptyCell"/>.
/// </summary>
[CreateAssetMenu(fileName = "Level_000", menuName = "Level/Level")]
public class LevelSO : ScriptableObject
{
    public const int EmptyCell = -1;

    #region Board
    [SerializeField] private PaletteSO palette;
    [SerializeField] private int width;
    [SerializeField] private int height;
    [SerializeField] private int[] cells = new int[0];
    #endregion

    #region Shooters
    [SerializeField] private List<ColumnSpec> columns = new List<ColumnSpec>();
    [Min(1)] [SerializeField] private int conveyorSlots = 5;
    [Min(1)] [SerializeField] private int cacheSlots = 5;
    #endregion

    #region Generator (để generate lại từ ảnh gốc)
    [SerializeField] private string sourceTexturePath;
    [SerializeField] private int generatorColumnCount = 4;
    [SerializeField] private int[] generatorAmmoSteps = { 10, 20, 30 };
    [SerializeField] private int seed;
    #endregion

    public PaletteSO Palette => palette;
    public int Width => width;
    public int Height => height;
    public IReadOnlyList<int> Cells => cells;
    public IReadOnlyList<ColumnSpec> Columns => columns;
    public int ConveyorSlots => conveyorSlots;
    public int CacheSlots => cacheSlots;

    public string SourceTexturePath => sourceTexturePath;
    public int GeneratorColumnCount => generatorColumnCount;
    public IReadOnlyList<int> GeneratorAmmoSteps => generatorAmmoSteps;
    public int Seed => seed;

    public int GetCell(int x, int y)
    {
        if (x < 0 || y < 0 || x >= width || y >= height) return EmptyCell;
        return cells[y * width + x];
    }

    public void SetBoard(PaletteSO newPalette, int newWidth, int newHeight, int[] newCells)
    {
        palette = newPalette;
        width = newWidth;
        height = newHeight;
        cells = newCells;
    }

    public void SetColumns(List<ColumnSpec> newColumns, int newConveyorSlots, int newCacheSlots)
    {
        columns = newColumns;
        conveyorSlots = newConveyorSlots;
        cacheSlots = newCacheSlots;
    }

    public void SetGeneratorParams(string texturePath, int columnCount, int[] ammoSteps, int newSeed)
    {
        sourceTexturePath = texturePath;
        generatorColumnCount = columnCount;
        generatorAmmoSteps = ammoSteps;
        seed = newSeed;
    }
}
