using System;

public class SlimeLocalEvent
{
    public event Action<int> OnShoot;

    public void Raise(int amount)
    {
        OnShoot?.Invoke(amount);
    }
}