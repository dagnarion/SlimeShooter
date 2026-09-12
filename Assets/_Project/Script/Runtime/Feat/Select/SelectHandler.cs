using System;
using UnityEngine;

public class SelectHandler : MonoBehaviour
{
    [SerializeField] private SelectionEventChannel selectionEvent;
    [SerializeField] private Camera mainCamera;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            selectionEvent?.EventRaise(mainCamera.ScreenToWorldPoint(Input.mousePosition));
        }
    }
}
