using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class PanelChanging : MonoBehaviour
{
    [SerializeField] private UIType2RectTransform[] rectTransforms;
    [SerializeField] float duration;
    
    private Dictionary<UIType, RectTransform> panel2Transforms = new Dictionary<UIType, RectTransform>();
    private UIType currentPanel = UIType.Home;
    private Tween slideTween;
    
    private float Width => ((RectTransform)transform).rect.width;

    private void Start()
    {
        for (int i = 0; i < rectTransforms.Length; i++)
        {
            UIType type = rectTransforms[i].uiType;
            RectTransform rectTransform = rectTransforms[i].rectTransform;
            panel2Transforms.Add(type, rectTransform);
            rectTransform.anchoredPosition = (type == currentPanel ? Vector2.zero : Vector2.right * Width);
        }
    }

    public void SlidePanel(int type)
    {
        UIType uiType = (UIType)type;
        if (uiType == currentPanel) return;
        if (slideTween != null && slideTween.IsActive() && slideTween.IsPlaying()) return;
        
        int direction = (int)currentPanel < (int)uiType ? -1 : 1;
        
        RectTransform currentRect = panel2Transforms[currentPanel];
        RectTransform newPanel =  panel2Transforms[uiType];
        
        newPanel.anchoredPosition = Vector2.right * (-direction * Width);
        
        slideTween = DOTween.Sequence()
            .Join(currentRect.DOAnchorPos(Vector2.right * (direction * Width), duration).SetEase(Ease.Linear))
            .Join(newPanel.DOAnchorPos(Vector2.zero, duration).SetEase(Ease.Linear))
            .SetUpdate(true)
            .OnComplete(() => currentPanel = uiType);
    }

    private void OnDestroy()
    {
        slideTween.Kill();
    }
}

[Serializable]
public struct UIType2RectTransform
{
    public UIType uiType;
    public RectTransform rectTransform;
}