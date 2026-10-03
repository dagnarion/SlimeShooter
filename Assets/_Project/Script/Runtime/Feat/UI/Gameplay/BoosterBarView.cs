using System;
using R3;
using Reflex.Attributes;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>Thanh 4 booster ở đáy màn hình + gợi ý khi đang chọn mục tiêu (Pickup / ColorBomb).</summary>
public class BoosterBarView : MonoBehaviour
{
    [Serializable]
    private struct Slot
    {
        public BoosterType type;
        public Button button;
        public TMP_Text label;
        public TMP_Text count;
    }

    [SerializeField] private Slot[] slots;
    [SerializeField] private GameObject hintPanel;
    [SerializeField] private TMP_Text hintText;
    [SerializeField] private Button cancelButton;

    [Inject] private BoosterService _boosters;
    [Inject] private BoosterConfigSO _config;
    [Inject] private GameSession _session;
    [Inject] private EndRushSystem _endRush;

    private void Start()
    {
        foreach (var slot in slots)
        {
            var type = slot.type;
            if (slot.label != null) slot.label.text = _config.Get(type).displayName;
            slot.button.onClick.AddListener(() => _boosters.Use(type));
            _boosters.Count(type).Subscribe(count => { if (slot.count != null) slot.count.text = count.ToString(); }).AddTo(this);
        }

        if (cancelButton != null) cancelButton.onClick.AddListener(_boosters.CancelMode);

        // Cập nhật trạng thái nút khi bất kỳ điều kiện nào thay đổi.
        Observable.Merge(
                _session.State.AsUnitObservable(),
                _endRush.IsActive.AsUnitObservable(),
                _boosters.ActiveMode.AsUnitObservable(),
                _boosters.OnUsed.AsUnitObservable())
            .Subscribe(_ => Refresh())
            .AddTo(this);
    }

    private void Refresh()
    {
        foreach (var slot in slots)
        {
            slot.button.interactable = _boosters.CanUse(slot.type);
        }

        var mode = _boosters.ActiveMode.CurrentValue;
        if (hintPanel != null) hintPanel.SetActive(mode.HasValue);
        if (hintText != null && mode.HasValue)
        {
            hintText.text = mode.Value == BoosterType.Pickup
                ? "Chạm vào shooter bất kỳ trong cột"
                : "Chạm vào shooter để xoá cả màu đó";
        }
    }
}
