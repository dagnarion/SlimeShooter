using Reflex.Attributes;
using UnityEngine;
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
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        if (!Physics.Raycast(ray, out RaycastHit hit, maxDistance, shooterLayer)) return;

        var view = hit.collider.GetComponentInParent<ShooterView>();
        if (view != null && view.Model != null) _pickService.TryPick(view.Model);
    }
}
