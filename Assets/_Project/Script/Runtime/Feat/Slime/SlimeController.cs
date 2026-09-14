using System;
using UnityEngine;

public class SlimeController : MonoBehaviour,IGamePices
{
    private SlimeLocalEvent slimeEvent;
    public Transform Transform { get; private set; } 
    public IMovable Movement { get; private set; }
    [SerializeField] private SlimeRender slimeRender;
    [SerializeField] private SlimeShooter slimeShooter;
    [SerializeField] private ComponentPoolSO<SlimeController> slimePool;
    private SlimeDataSO slimeData;


    private void OnDisable()
    {
       if(slimeEvent!=null) slimeEvent.OnDead -= Dead;
    }

    public void Init(SlimeDataSO slimeData)
    {
        slimeEvent = new SlimeLocalEvent();
        slimeRender.Init(slimeData.Color,slimeData.bulletAmount,slimeEvent);
        slimeShooter.Setup(slimeData.Color,slimeData.bulletAmount,slimeEvent);
        Movement = new SlimeMovement(transform);
        Transform = this.gameObject.transform;
        slimeEvent.OnDead += Dead;
    }

    private void Dead()
    {
        slimePool.Release(this);
    }


    
}