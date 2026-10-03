using Reflex.Attributes;
using UnityEngine;

/// <summary>MonoBehaviour duy nhất đẩy thời gian vào TickScheduler, chỉ khi đang Playing.</summary>
public class GameLoop : MonoBehaviour
{
    [Inject] private GameSession _session;
    [Inject] private TickScheduler _scheduler;

    private void Update()
    {
        if (_session == null || !_session.IsPlaying) return;
        _scheduler.Tick(Time.deltaTime);
    }
}
