using System;
using UnityEditor.Experimental.GraphView;
using UnityEngine;
using UnityEngine.InputSystem;

public class SelectHandler : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private SelectionEventChannel selectionEvent;
    [SerializeField] private InputActionReference clickAction;
    [SerializeField] private InputActionReference clickPosition;
    [SerializeField] private LayerMask checkingLayer;
    
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
        Vector2 mousePos = clickPosition.action.ReadValue<Vector2>();
        RaycastHit ray;
        Physics.Raycast(mainCamera.ScreenPointToRay(mousePos), out ray, Mathf.Infinity,checkingLayer);
        if(ray.collider == null) return;
        selectionEvent?.EventRaise(ray.point);
    }
}
