using UnityEngine;
using UnityEngine.Splines;

public class ConveyorManager : MonoBehaviour
{
    #region Fields
    [SerializeField] private SplineContainer splineContainer;
    [SerializeField] private ConveyorDataSO data;
    [SerializeField] private MovableEventChanelSO onItemEntered;
    [SerializeField] private MovableEventChanelSO onItemExited;
    [SerializeField] private MovableEventChanelSO onItemDied;
    [SerializeField] private bool isEndgameRush = false;

    private int _currentBeltCapacity;
    private QueueService queueService;
    private BeltService beltService;

    #endregion

    #region Life Cycle

    private void Awake()
    {
        Vector3 entryPosition = GetBeltEntry();

        queueService = new QueueService(
            entryPosition,
            data.ItemHeight,
            data.DropDuration);

        beltService = new BeltService(
            splineContainer,
            entryPosition,
            data.StartPointInConveyor,
            data.EndPointInConveyor,
            data.MoveSpeed,
            data.SafeDistance,
            data.JumpDuration);
    }

    private void Start()
    {
        if (data != null)
        {
            _currentBeltCapacity = data.MaxBeltCapacity;
        }
    }

    private void Update()
    {
        beltService.Tick(Time.deltaTime, isEndgameRush, unit =>
        {
            onItemExited?.EventRaise(unit);
        });

        TryDispatchFromQueue();
    }

    #endregion

    public bool IsEndgameRush
    {
        get => isEndgameRush;
        set => isEndgameRush = value;
    }

    public void AddItemToConveyor(IGamePieces unit)
    {
        if (unit == null || !splineContainer || !CanAcceptToConveyor()) return;
        
        if (queueService.GetQueueCount() == 0 && beltService.IsEntranceClear(isEndgameRush))
        {
            beltService.AddDirect(unit);
        }
        else
        {
            queueService.AddToQueue(unit);
        }

        unit.Movement.OnAccepted?.Invoke();
    }

    public void RemoveItemFromConveyor(IGamePieces unit)
    {
        if (unit == null) return;

        if (beltService.IsBeltHasUnit(unit))
        {
            beltService.RemoveFromBelt(unit);
            TryDispatchFromQueue();
            return;
        }

        if (queueService.IsQueueHasUnit(unit))
        {
            queueService.RemoveFromQueue(unit);
        }
    }

    private void TryDispatchFromQueue()
    {
        if (queueService.GetQueueCount() == 0 || !beltService.IsEntranceClear(isEndgameRush)) return;
        
        var unit = queueService.GetFirstQueue();

        beltService.AddFromQueue(unit);
        queueService.RestackQueue();
    }

    public bool CanAcceptToConveyor()
    {
        if (!splineContainer) return false;
        int maxCapacity = _currentBeltCapacity > 0 ? _currentBeltCapacity : (data != null ? data.MaxBeltCapacity : 5);
        return (beltService.GetBeltCount() + queueService.GetQueueCount()) < maxCapacity;
    }

    private Vector3 GetBeltEntry()
    {
        splineContainer.Evaluate(data.StartPointInConveyor, out var pos, out _, out _);
        return pos;
    }
}