using R3;
using Reflex.Attributes;
using UnityEngine;

/// <summary>
/// Điểm vào của scene gameplay. P1: chỉ đưa session sang Playing.
/// Từ P2 sẽ load level qua LevelService trước khi StartPlaying.
/// </summary>
public class GameplayEntryPoint : MonoBehaviour
{
    [SerializeField] private bool logStateChanges = true;

    [Inject] private GameSession _session;

    private void Start()
    {
        if (logStateChanges)
        {
            _session.State
                .Subscribe(state => Debug.Log($"[GameSession] State = {state}"))
                .AddTo(this);
        }

        _session.StartPlaying();
    }
}
