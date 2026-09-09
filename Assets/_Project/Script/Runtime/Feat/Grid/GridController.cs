using System;
using NaughtyAttributes;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] private GridDataSO data;
    [SerializeField] private GridRender render;
    [SerializeField] private GameObject slimePrefab;
    [SerializeField] private Grid gridComponent;
    [SerializeField] private Transform holder;
    private Grid<GameObject> grid;
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
}
