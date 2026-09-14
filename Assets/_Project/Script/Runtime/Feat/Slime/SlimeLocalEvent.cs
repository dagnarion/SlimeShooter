using System;

public class SlimeLocalEvent
{
    public event Action<int> OnShoot;
    public event Action OnDead;
    public void Raise(int amount)
    {
        OnShoot?.Invoke(amount);
    }

    public void RaiseDead()
    {
        OnDead?.Invoke();
    }
}