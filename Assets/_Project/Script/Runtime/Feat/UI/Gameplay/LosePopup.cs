using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LosePopup : UIPopup
{
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button reviveButton;
    [SerializeField] private Button retryButton;

    [Inject] private GameFlowService _flow;
    [Inject] private ReviveService _revive;

    protected override void Awake()
    {
        base.Awake();
        reviveButton.onClick.AddListener(() => _flow.Revive());
        retryButton.onClick.AddListener(() => _flow.Retry());
    }

    protected override void OnShown()
    {
        if (messageText != null) messageText.text = "OUT OF SPACE!";
        reviveButton.gameObject.SetActive(_revive.CanRevive);
    }
}
