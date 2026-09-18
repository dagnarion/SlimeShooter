using DG.Tweening;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class ButtonPunchScale : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    [Header("References")]
    [SerializeField] private Button button;

    [Header("Punch Settings")]
    [SerializeField] private float punchStrength = 0.15f;
    [SerializeField] private float punchDuration = 0.35f;
    [SerializeField] private int vibrato = 6;
    [SerializeField] private float elasticity = 0.7f;

    [Header("Press Settings")]
    [SerializeField] private float pressScale = 0.9f;
    [SerializeField] private float pressDuration = 0.12f;

    private Transform targetTransform;
    private Vector3 originalScale;
    private Tween scaleTween;
    private bool isPressed;

    private void Awake()
    {
        if (button == null)
            button = GetComponent<Button>();

        targetTransform = button.transform;
        originalScale = targetTransform.localScale;

        button.onClick.AddListener(OnTap);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (!button.interactable) return;

        isPressed = true;
        scaleTween?.Kill();
        scaleTween = targetTransform
            .DOScale(originalScale * pressScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        if (!isPressed) return;
        isPressed = false;

        scaleTween?.Kill();
        scaleTween = targetTransform
            .DOScale(originalScale, pressDuration)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void OnTap()
    {
        scaleTween?.Kill();
        targetTransform.localScale = originalScale;

        scaleTween = targetTransform
            .DOPunchScale(Vector3.one * punchStrength, punchDuration, vibrato, elasticity)
            .SetEase(Ease.OutQuad)
            .SetUpdate(true);
    }

    private void OnDestroy()
    {
        scaleTween?.Kill();
        button.onClick.RemoveListener(OnTap);
    }
}