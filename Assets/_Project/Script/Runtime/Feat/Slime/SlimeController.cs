using UnityEngine;

public class SlimeController : MonoBehaviour,IGamePices
{
    private SlimeLocalEvent slimeEvent;
    public Transform Transform { get; private set; } 
    public IMovable Movement { get; private set; }
    [SerializeField] private SlimeRender slimeRender;
    [SerializeField] private SlimeShooter slimeShooter;
    private SlimeDataSO slimeData;

    
    public void Init(SlimeDataSO slimeData)
    {
        slimeEvent = new SlimeLocalEvent();
        slimeRender.Init(slimeData.Color,slimeData.bulletAmount,slimeEvent);
        slimeShooter.Setup(slimeData.Color,slimeData.bulletAmount,slimeEvent);
        Movement = new SlimeMovement(transform);
        Transform = this.gameObject.transform;
    }


    
}