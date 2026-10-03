using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Hình chữ nhật bo góc trên mặt phẳng XZ bao quanh board. Bắt đầu ở góc dưới-trái (cửa vào),
/// chạy ngược chiều kim đồng hồ khi nhìn từ trên xuống: dưới (+X) → phải (+Z) → trên (-X) → trái (-Z).
/// Phía bên trái của hướng đi luôn là phía trong (board).
/// </summary>
public sealed class RectConveyorPath : IConveyorPath
{
    private struct Segment
    {
        public float Start;
        public float Length;
        public bool IsArc;
        public bool HasSide;
        public BoardSide Side;
        // Line
        public Vector3 From;
        public Vector3 Direction;
        // Arc
        public Vector3 Center;
        public float StartAngle; // radian, đo từ +X về +Z
    }

    private readonly List<Segment> _segments = new List<Segment>();
    private readonly float _radius;

    public RectConveyorPath(Vector3 center, float halfWidth, float halfDepth, float cornerRadius)
    {
        _radius = Mathf.Clamp(cornerRadius, 0f, Mathf.Min(halfWidth, halfDepth));
        float hw = halfWidth, hd = halfDepth, r = _radius;
        float y = center.y;
        Vector3 C(float x, float z) => new Vector3(center.x + x, y, center.z + z);

        AddLine(C(-hw + r, -hd), Vector3.right, 2f * (hw - r), BoardSide.Bottom);
        AddArc(C(hw - r, -hd + r), -90f);
        AddLine(C(hw, -hd + r), Vector3.forward, 2f * (hd - r), BoardSide.Right);
        AddArc(C(hw - r, hd - r), 0f);
        AddLine(C(hw - r, hd), Vector3.left, 2f * (hw - r), BoardSide.Top);
        AddArc(C(-hw + r, hd - r), 90f);
        AddLine(C(-hw, hd - r), Vector3.back, 2f * (hd - r), BoardSide.Left);
        AddArc(C(-hw + r, -hd + r), 180f);

        Bounds = new Bounds(center, new Vector3(hw * 2f, 0f, hd * 2f));
    }

    public float Length { get; private set; }
    public Bounds Bounds { get; }

    public Vector3 Evaluate(float distance, out Vector3 forward)
    {
        var segment = Find(Wrap(distance), out float local);
        if (!segment.IsArc)
        {
            forward = segment.Direction;
            return segment.From + segment.Direction * local;
        }

        float angle = segment.StartAngle + (_radius > 0f ? local / _radius : 0f);
        forward = new Vector3(-Mathf.Sin(angle), 0f, Mathf.Cos(angle));
        return segment.Center + new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle)) * _radius;
    }

    public bool TryGetSide(float distance, out BoardSide side)
    {
        var segment = Find(Wrap(distance), out _);
        side = segment.Side;
        return segment.HasSide;
    }

    /// <summary>Hướng vào trong (bên trái hướng đi).</summary>
    public static Vector3 Inward(Vector3 forward) => Vector3.Cross(forward, Vector3.up);

    private float Wrap(float distance)
    {
        if (Length <= 0f) return 0f;
        distance %= Length;
        return distance < 0f ? distance + Length : distance;
    }

    private Segment Find(float distance, out float local)
    {
        for (int i = 0; i < _segments.Count; i++)
        {
            var s = _segments[i];
            if (distance < s.Start + s.Length || i == _segments.Count - 1)
            {
                local = Mathf.Clamp(distance - s.Start, 0f, s.Length);
                return s;
            }
        }
        local = 0f;
        return default;
    }

    private void AddLine(Vector3 from, Vector3 direction, float length, BoardSide side)
    {
        if (length <= 0f) return;
        _segments.Add(new Segment { Start = Length, Length = length, From = from, Direction = direction, HasSide = true, Side = side });
        Length += length;
    }

    private void AddArc(Vector3 center, float startAngleDegrees)
    {
        float length = _radius * Mathf.PI * 0.5f;
        if (length <= 0f) return;
        _segments.Add(new Segment { Start = Length, Length = length, IsArc = true, Center = center, StartAngle = startAngleDegrees * Mathf.Deg2Rad });
        Length += length;
    }
}
