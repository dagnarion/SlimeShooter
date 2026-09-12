using System;
using DG.Tweening;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Splines;

public class SlimeMove : IMovable
{
    private readonly Transform _transform;
    
    private float _distance;
    private float _splineLength;
    private float _exitProgress;
    private float _startProgress;
    
    private Tween _jumpTween;
    private Tween _dropTween;
    
    public float Progress => _splineLength > 0.001f ? _distance / _splineLength : 0f;

    public SlimeMove(Transform transform)
    {
        _transform = transform;
    }
    
    public bool Tick(float deltaTime, float speed, SplineContainer container, bool isLooping)
    {
        if (!container || _splineLength <= 0.001f) return false;
        
        _distance += deltaTime * speed;
        bool completedLap = false;

        if (_distance >= _exitProgress * _splineLength)
        {
            completedLap = true;
            HandleEndConveyor(isLooping);
        }
        
        float t = _distance / _splineLength;
        container.Evaluate(t, out float3 worldPos, out float3 worldTangent, out float3 worldUp);
        
        _transform.position = worldPos;
        
        if (math.lengthsq(worldTangent) <= 0.0001f) return completedLap;

        Vector3 tangent = math.normalize(worldTangent);
        Vector3 up = math.lengthsq(worldUp) > 0.0001f ? math.normalize((Vector3)worldUp) : Vector3.up;
        Vector3 inwardDirection = Vector3.Cross(up, tangent);
        if (inwardDirection.sqrMagnitude > 0.001f)
        {
            _transform.rotation = Quaternion.LookRotation(inwardDirection, up);
        }
        return completedLap;
    }

    public float GetDistanceOnBelt()
    {
        return _distance;
    }

    public void PlayJumpTo(Vector3 targetPos, float duration, Action onComplete = null)
    {
        _jumpTween?.Kill();
        _jumpTween = _transform.DOJump(targetPos, jumpPower: 1.2f, numJumps: 1, duration: duration)
            .SetEase(Ease.OutQuad)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void PlayDropTo(Vector3 targetPos, float duration, Action onComplete = null)
    {
        _dropTween?.Kill();
        _dropTween = _transform.DOMove(targetPos, duration)
            .SetEase(Ease.OutBounce)
            .OnComplete(() => onComplete?.Invoke());
    }

    public void AttachToBelt(SplineContainer container, float startProgress = 0f, float exitProgress = 1f)
    {
        _splineLength = container ? container.CalculateLength() : 0f;
        _startProgress = Mathf.Clamp01(startProgress);
        _exitProgress = Mathf.Clamp(Mathf.Max(_startProgress, exitProgress), _startProgress, 1f);
        _distance = _startProgress * _splineLength;
    }

    private void HandleEndConveyor(bool isLooping = false)
    {
        if (isLooping)
        {
            _distance = _startProgress * _splineLength;
        }
        else
        {
            _distance = _exitProgress *  _splineLength;
        }
    }
}
