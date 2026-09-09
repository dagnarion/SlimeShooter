using System;
using NaughtyAttributes;
using UnityEngine;

public class GridController : MonoBehaviour
{
    [SerializeField] private GridDataSO data;
    [SerializeField] private GridRender render;

    private void Start()
    {
        Init();
    }

    [Button]
    private void Init()
    {
        render.Init(data);
    }
}
