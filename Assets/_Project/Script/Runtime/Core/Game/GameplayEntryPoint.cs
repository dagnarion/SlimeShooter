using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>
/// Điểm vào của scene gameplay: lấy level hiện tại rồi đưa session sang Playing.
/// Từ P3/P4 sẽ dựng board và cột shooter từ level trước khi StartPlaying.
/// </summary>
public class GameplayEntryPoint : MonoBehaviour
{
    [SerializeField] private bool logStateChanges = true;

    [Inject] private GameSession _session;
    [Inject] private LevelService _levelService;
    [Inject] private ShooterPickService _pickService;

    private void Start()
    {
        if (logStateChanges)
        {
            _session.State
                .Subscribe(state => Debug.Log($"[GameSession] State = {state}"))
                .AddTo(this);
            _pickService.OnPicked
                .Subscribe(shooter => Debug.Log($"[Pick] {shooter}"))
                .AddTo(this);
        }

        var level = _levelService.Current;
        var validation = LevelValidator.Validate(level);
        if (!validation.IsValid)
        {
            Debug.LogError($"[Gameplay] Level {_levelService.DisplayNumber} ({(level != null ? level.name : "null")}) không hợp lệ:\n{validation}");
            return;
        }

        Debug.Log($"[Gameplay] Level {_levelService.DisplayNumber}: {level.name} ({level.Width}x{level.Height}, {level.Columns.Count} cột)");
        _session.StartPlaying();
    }
}
