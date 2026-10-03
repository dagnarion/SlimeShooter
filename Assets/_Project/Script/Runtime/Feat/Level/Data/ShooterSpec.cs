using System;
using System.Collections.Generic;

[Serializable]
public struct ShooterSpec
{
    public int colorId;
    public int ammo;

    public ShooterSpec(int colorId, int ammo)
    {
        this.colorId = colorId;
        this.ammo = ammo;
    }
}

/// <summary>Một cột shooter. Phần tử index 0 là shooter đứng đầu (chọn được đầu tiên).</summary>
[Serializable]
public class ColumnSpec
{
    public List<ShooterSpec> shooters = new List<ShooterSpec>();
}
