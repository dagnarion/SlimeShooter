using UnityEngine;
using UnityEngine.Splines;
using NUnit.Framework;

public class SlimeMovementTest
{
    private GameObject _slimeGameObject;
    private Transform _slimeTransform;
    private GameObject _splineGameObject;
    private SplineContainer _splineContainer;
    private SlimeMovement _slimeMove;

    [SetUp]
    public void SetUp()
    {
        _slimeGameObject = new GameObject("Test_Slime_Movement");
        _slimeTransform = _slimeGameObject.transform;
        _slimeMove = new SlimeMovement(_slimeTransform);
        
        _splineGameObject = new GameObject("Test_Spline");
        _splineContainer = _splineGameObject.AddComponent<SplineContainer>();
        
        var spline = _splineContainer.Spline;
        spline.Clear();
        spline.Add(new BezierKnot(new Unity.Mathematics.float3(0, 0, 0)));
        spline.Add(new BezierKnot(new Unity.Mathematics.float3(0, 0, 10)));
    }

    [TearDown]
    public void TearDown()
    {
        _slimeMove.Dispose();
        
        if (_slimeGameObject != null) Object.DestroyImmediate(_slimeGameObject);
        if (_splineGameObject != null) Object.DestroyImmediate(_splineGameObject);
    }

    [Test]
    public void AttachToBelt_ValidContainer_SetsIsAttachedTrueAndCalculatesProgress()
    {
        _slimeMove.AttachToBelt(_splineContainer, startProgress: 0.2f, exitProgress: 0.8f);
        
        Assert.IsTrue(_slimeMove.IsAttached);
        Assert.AreEqual(0.2f, _slimeMove.Progress, 0.01f);
    }
    
    [Test]
    public void AttachToBelt_NegativeOrOverProgress_ClampsValuesProperly()
    {
        _slimeMove.AttachToBelt(_splineContainer, startProgress: -0.5f, exitProgress: 1.5f);
        Assert.AreEqual(0f, _slimeMove.Progress, 0.001f);
    }
    
    [Test]
    public void Tick_MovesSlimeForwardByDeltaTimeAndSpeed()
    {
        _slimeMove.AttachToBelt(_splineContainer, startProgress: 0f, exitProgress: 1f);
        float deltaTime = 0.5f;
        float speed = 2.0f;

        bool lapCompleted = _slimeMove.Tick(deltaTime, speed, _splineContainer, isLooping: false);

        Assert.IsFalse(lapCompleted);
        Assert.AreEqual(1.0f, _slimeMove.GetDistanceOnBelt(), 0.01f);
    }
    
    [Test]
    public void Tick_ReachedEnd_WhenLooping_ResetsToStartProgress()
    {
        float startProgress = 0.1f;
        float exitProgress = 0.5f;
        _slimeMove.AttachToBelt(_splineContainer, startProgress, exitProgress);
        
        float splineLength = _splineContainer.CalculateLength();
        float distanceToEnd = (exitProgress - startProgress) * splineLength;

        bool lapCompleted = _slimeMove.Tick(deltaTime: 1f, speed: distanceToEnd + 1f, _splineContainer, isLooping: true);

        Assert.IsTrue(lapCompleted, "Tick phải báo đã hoàn thành vòng khi qua exitProgress");
        Assert.AreEqual(startProgress * splineLength, _slimeMove.GetDistanceOnBelt(), 0.05f, 
            "Khi looping = true, vị trí phải reset về startProgress");
    }
    [Test]
    public void Tick_ReachedEnd_WhenNotLooping_ClampsAtExitProgress()
    {
        float startProgress = 0f;
        float exitProgress = 0.8f;
        _slimeMove.AttachToBelt(_splineContainer, startProgress, exitProgress);
        
        float splineLength = _splineContainer.CalculateLength();

        bool lapCompleted = _slimeMove.Tick(deltaTime: 10f, speed: 50f, _splineContainer, isLooping: false);

        Assert.IsTrue(lapCompleted);
        Assert.AreEqual(exitProgress * splineLength, _slimeMove.GetDistanceOnBelt(), 0.05f,
            "Khi không looping, khoảng cách phải dừng tại exitProgress");
    }
    [Test]
    public void Tick_NullContainer_ReturnsFalse()
    {
        bool result = _slimeMove.Tick(0.1f, 1f, container: null, isLooping: false);

        Assert.IsFalse(result);
    }
}
