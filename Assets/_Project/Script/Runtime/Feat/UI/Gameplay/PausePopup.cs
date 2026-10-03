using Reflex.Attributes;
using UnityEngine;
using UnityEngine.UI;

public class PausePopup : UIPopup
{
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button retryButton;

    [Inject] private GameFlowService _flow;

    protected override void Awake()
    {
        base.Awake();
        resumeButton.onClick.AddListener(() => _flow.Resume());
        retryButton.onClick.AddListener(() => _flow.Retry());
    }
}
