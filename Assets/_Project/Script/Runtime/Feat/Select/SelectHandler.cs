using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SelectionEventChannel selectionEvent;
    [SerializeField] private InputActionReference clickAction;

    private void OnEnable()
    {
        clickAction.action.Enable();
        clickAction.action.performed += OnClick;
    }

    private void OnDisable()
    {
        clickAction.action.performed -= OnClick;
        clickAction.action.Disable();
    }

    private void OnClick(InputAction.CallbackContext ctx)
    {
        Vector3 mousePos = Mouse.current.position.ReadValue();
        selectionEvent?.EventRaise(mainCamera.ScreenToWorldPoint(mousePos));
    }
}
