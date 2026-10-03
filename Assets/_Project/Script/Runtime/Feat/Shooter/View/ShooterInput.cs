using Reflex.Attributes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

/// <summary>Raycast vào collider của shooter rồi chuyển cho ShooterPickService quyết định.</summary>
public class ShooterInput : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference pointerPosition;
    [SerializeField] private LayerMask shooterLayer;
    [SerializeField] private float maxDistance = 200f;

    [Inject] private ShooterPickService _pickService;

    private void OnEnable()
    {
        clickAction.action.Enable();
        pointerPosition.action.Enable();
        clickAction.action.performed += OnClick;
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClick;
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        Vector2 screenPosition = pointerPosition.action.ReadValue<Vector2>();
        if (IsOverUI(screenPosition)) return;

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, shooterLayer)) return;

        var view = hit.collider.GetComponentInParent<ShooterView>();
        if (view != null && view.Model != null) _pickService.TryPick(view.Model);
    }

    private readonly System.Collections.Generic.List<RaycastResult> _uiHits = new System.Collections.Generic.List<RaycastResult>();

    /// <summary>Click trúng nút/popup UI thì không chọn shooter phía dưới.</summary>
    private bool IsOverUI(Vector2 screenPosition)
    {
        var eventSystem = EventSystem.current;
        if (eventSystem == null) return false;
        var data = new PointerEventData(eventSystem) { position = screenPosition };
        _uiHits.Clear();
        eventSystem.RaycastAll(data, _uiHits);
        return _uiHits.Count > 0;
    }
}
