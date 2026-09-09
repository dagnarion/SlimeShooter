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
        render.Init(gridComponent,data);
        grid = new Grid<GameObject>(data.GridSize, Pos =>
        {
            GameObject slime = Instantiate(slimePrefab,gridComponent.GetCellCenterWorld(new Vector3Int(Pos.x,Pos.y,0)),Quaternion.identity,holder);
            return slime;
        });
    }

    private void Choose(Vector3 pos)
    {
        Vector3Int position = gridComponent.WorldToCell(pos);
        Debug.Log(position);
    }
}
