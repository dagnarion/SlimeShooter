using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Một material dùng chung cho mỗi color id (clone từ material gốc).
/// Thay cho <c>renderer.material.color = ...</c> (tạo material riêng mỗi object, phá batching).
/// </summary>
public class PaletteMaterialCache : IDisposable
{
    private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
    private static readonly int ColorId = Shader.PropertyToID("_Color");

    private readonly Material _baseMaterial;
    private readonly PaletteSO _palette;
    private readonly Dictionary<int, Material> _materials = new Dictionary<int, Material>();

    public PaletteMaterialCache(Material baseMaterial, PaletteSO palette)
    {
        _baseMaterial = baseMaterial;
        _palette = palette;
    }

    public PaletteSO Palette => _palette;

    public Material Get(int colorId)
    {
        if (_materials.TryGetValue(colorId, out var material)) return material;

        material = new Material(_baseMaterial) { name = $"{_baseMaterial.name}_{colorId}" };
        Color color = _palette != null ? (Color)_palette.GetColor(colorId) : Color.magenta;
        if (material.HasProperty(BaseColorId)) material.SetColor(BaseColorId, color);
        if (material.HasProperty(ColorId)) material.SetColor(ColorId, color);

        _materials[colorId] = material;
        return material;
    }

    public void Dispose()
    {
        foreach (var material in _materials.Values)
        {
            if (material == null) continue;
            if (Application.isPlaying) UnityEngine.Object.Destroy(material);
            else UnityEngine.Object.DestroyImmediate(material);
        }
        _materials.Clear();
    }
}
