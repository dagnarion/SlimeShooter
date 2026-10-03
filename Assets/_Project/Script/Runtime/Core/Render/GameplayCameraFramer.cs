using Reflex.Attributes;
using UnityEngine;

/// <summary>
/// Căn camera (góc nghiêng cố định) sao cho băng chuyền + khu cột shooter vừa khung hình,
/// bất kể kích thước level hay tỉ lệ màn hình.
/// </summary>
[DefaultExecutionOrder(100)]
public class GameplayCameraFramer : MonoBehaviour
{
    [SerializeField] private Camera targetCamera;
    [SerializeField] private ShooterColumnsView columnsView;
    [Range(30f, 90f)] [SerializeField] private float pitch = 62f;
    [Range(0f, 0.2f)] [SerializeField] private float viewportPadding = 0.04f;
    [Min(1)] [SerializeField] private int visibleColumnRows = 3;
    [SerializeField] private float extraTopMargin = 0.6f;

    [Inject] private IConveyorPath _path;

    private void Start() => Frame();

    public void Frame()
    {
        Camera cam = targetCamera != null ? targetCamera : Camera.main;
        if (cam == null) return;

        Bounds area = _path.Bounds;
        area.Encapsulate(area.max + Vector3.forward * extraTopMargin);
        if (columnsView != null) area.Encapsulate(columnsView.GetBounds(visibleColumnRows));

        Quaternion rotation = Quaternion.Euler(pitch, 0f, 0f);
        Vector3 forward = rotation * Vector3.forward;
        Vector3[] corners =
        {
            new Vector3(area.min.x, 0f, area.min.z), new Vector3(area.max.x, 0f, area.min.z),
            new Vector3(area.min.x, 0f, area.max.z), new Vector3(area.max.x, 0f, area.max.z)
        };

        cam.transform.rotation = rotation;
        float low = 1f, high = 300f;
        for (int i = 0; i < 40; i++)
        {
            float mid = (low + high) * 0.5f;
            cam.transform.position = area.center - forward * mid;
            if (AllInside(cam, corners)) high = mid; else low = mid;
        }
        cam.transform.position = area.center - forward * high;
    }

    private bool AllInside(Camera cam, Vector3[] points)
    {
        foreach (var p in points)
        {
            Vector3 v = cam.WorldToViewportPoint(p);
            if (v.z <= 0f || v.x < viewportPadding || v.x > 1f - viewportPadding || v.y < viewportPadding || v.y > 1f - viewportPadding)
                return false;
        }
        return true;
    }
}
