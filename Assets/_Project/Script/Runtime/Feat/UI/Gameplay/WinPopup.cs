using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WinPopup : UIPopup
{
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private Button nextButton;

    [Inject] private GameFlowService _flow;
    [Inject] private LevelProgressSystem _progress;

    protected override void Awake()
    {
        base.Awake();
        nextButton.onClick.AddListener(() => _flow.Next());
    }

    protected override void OnShown()
    {
        if (titleText != null) titleText.text = $"LEVEL {_progress.PlayedLevelNumber}\nCOMPLETE!";
    }
}
