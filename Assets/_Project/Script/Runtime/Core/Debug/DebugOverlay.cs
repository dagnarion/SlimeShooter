using Reflex.Attributes;
using UnityEngine;

/// <summary>Thông số gameplay góc trên màn hình — chỉ trong Editor và Development build.</summary>
public class DebugOverlay : MonoBehaviour
{
    [SerializeField] private bool visible = true;

    [Inject] private GameSession _session;
    [Inject] private PixelBoard _board;
    [Inject] private ConveyorModel _conveyor;
    [Inject] private CacheTray _tray;
    [Inject] private EndRushSystem _endRush;
    [Inject] private RuleSystem _rules;
    [Inject] private LevelService _levels;

#if UNITY_EDITOR || DEVELOPMENT_BUILD
    private GUIStyle _style;

    private void OnGUI()
    {
        if (!visible || _session == null) return;
        _style ??= new GUIStyle(GUI.skin.label) { fontSize = Mathf.Max(14, Screen.height / 60), normal = { textColor = Color.white } };

        string text =
            $"Level {_levels.DisplayNumber}  [{_session.CurrentState}]{(_endRush.IsActive.CurrentValue ? "  RUSH" : "")}\n" +
            $"Pixel {_board.Remaining.CurrentValue}/{_board.TotalCount} (alive {_board.AliveCount})\n" +
            $"Belt {_conveyor.Units.Count}/{_conveyor.Capacity.CurrentValue}   Tray {_tray.Count}/{_tray.Capacity.CurrentValue}\n" +
            (_rules.LoseReason != LoseReason.None ? $"Lose: {_rules.LoseReason}" : "");
        GUI.Label(new Rect(10, 10, Screen.width - 20, Screen.height * 0.2f), text, _style);
    }
#endif
}
