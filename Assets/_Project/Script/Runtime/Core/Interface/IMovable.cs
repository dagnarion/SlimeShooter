using System;
using UnityEngine;
using UnityEngine.Splines;

public interface IMovable
{
    public GameObject owner { get; }
    public Transform Transform { get; }
    public bool IsAttached { get; }
    public Action OnAccepted { get; set; }
    public bool Tick(float deltaTime, float speed, SplineContainer container, bool isLooping);
    public float GetDistanceOnBelt();
    public void PlayJumpTo(Vector3 targetPos, float duration, Action onComplete = null);
    public void PlayDropTo(Vector3 targetPos, float duration, Action onComplete = null);
    public void AttachToBelt(SplineContainer container, float startProgress = 0f, float exitProgress = 1f);
}
