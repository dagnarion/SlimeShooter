using System.Collections.Generic;
using DG.Tweening;
using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>Cụm phao cạnh cửa vào: mỗi phao = 1 slot băng chuyền còn trống (giống bộ đếm x/N).</summary>
public class BuoyStackView : MonoBehaviour
{
    [SerializeField] private Material buoyMaterial;
    [SerializeField] private float radius = 0.32f;
    [SerializeField] private float tube = 0.11f;
    [SerializeField] private Vector3 offsetFromEntrance = new Vector3(-1.3f, 0f, 0.9f);
    [Tooltip("Lệch theo -Z để nhìn từ camera nghiêng vẫn đếm được từng phao.")]
    [SerializeField] private Vector3 stackStep = new Vector3(0f, 0.06f, -0.3f);

    [Inject] private ConveyorModel _conveyor;

    private readonly List<Transform> _buoys = new List<Transform>();
    private Mesh _mesh;

    private void Start()
    {
        _mesh = ProceduralMeshes.Torus(radius, tube);
        transform.position = _conveyor.Path.Evaluate(0f, out _) + offsetFromEntrance;

        _conveyor.Capacity.Subscribe(EnsureCount).AddTo(this);
        _conveyor.Available.Subscribe(Show).AddTo(this);
    }

    private void EnsureCount(int capacity)
    {
        while (_buoys.Count < capacity)
        {
            var buoy = new GameObject($"Buoy_{_buoys.Count}");
            buoy.transform.SetParent(transform, false);
            buoy.transform.localPosition = stackStep * _buoys.Count;
            buoy.transform.localRotation = Quaternion.Euler(0f, _buoys.Count * 23f, 0f);
            buoy.AddComponent<MeshFilter>().sharedMesh = _mesh;
            var renderer = buoy.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = buoyMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            _buoys.Add(buoy.transform);
        }
        Show(_conveyor.Available.CurrentValue);
    }

    private void Show(int available)
    {
        for (int i = 0; i < _buoys.Count; i++)
        {
            bool visible = i < available;
            var buoy = _buoys[i];
            if (buoy.gameObject.activeSelf == visible) continue;
            buoy.DOKill();
            if (visible)
            {
                buoy.gameObject.SetActive(true);
                buoy.localScale = Vector3.zero;
                buoy.DOScale(Vector3.one, 0.2f).SetEase(Ease.OutBack).SetLink(buoy.gameObject);
            }
            else
            {
                buoy.gameObject.SetActive(false);
            }
        }
    }

    private void OnDestroy()
    {
        if (_mesh != null) Destroy(_mesh);
    }
}
