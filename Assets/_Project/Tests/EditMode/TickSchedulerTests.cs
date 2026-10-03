using System;
using NUnit.Framework;

public class TickSchedulerTests
{
    private class Counter : ITickable
    {
        public float Total;
        public int Calls;
        public Action OnTick;

        public void Tick(float deltaTime)
        {
            Total += deltaTime;
            Calls++;
            OnTick?.Invoke();
        }
    }

    [Test]
    public void Tick_CallsRegistered_WithScaledDelta()
    {
        var scheduler = new TickScheduler { TimeScale = 2f };
        var counter = new Counter();
        scheduler.Register(counter);

        scheduler.Tick(0.5f);

        Assert.AreEqual(1f, counter.Total, 1e-5f);
        Assert.AreEqual(1, counter.Calls);
    }

    [Test]
    public void Tick_SkipsWhenTimeScaleZero()
    {
        var scheduler = new TickScheduler { TimeScale = 0f };
        var counter = new Counter();
        scheduler.Register(counter);

        scheduler.Tick(1f);

        Assert.AreEqual(0, counter.Calls);
    }

    [Test]
    public void Register_IsIdempotent()
    {
        var scheduler = new TickScheduler();
        var counter = new Counter();
        scheduler.Register(counter);
        scheduler.Register(counter);

        scheduler.Tick(1f);

        Assert.AreEqual(1, scheduler.Count);
        Assert.AreEqual(1, counter.Calls);
    }

    [Test]
    public void UnregisterDuringTick_TakesEffectImmediately()
    {
        var scheduler = new TickScheduler();
        var first = new Counter();
        var second = new Counter();
        first.OnTick = () => scheduler.Unregister(second);
        scheduler.Register(first);
        scheduler.Register(second);

        scheduler.Tick(1f);

        Assert.AreEqual(0, second.Calls);
        Assert.AreEqual(1, scheduler.Count);
    }

    [Test]
    public void RegisterDuringTick_StartsNextTick()
    {
        var scheduler = new TickScheduler();
        var first = new Counter();
        var late = new Counter();
        first.OnTick = () => scheduler.Register(late);
        scheduler.Register(first);

        scheduler.Tick(1f);
        Assert.AreEqual(0, late.Calls);

        scheduler.Tick(1f);
        Assert.AreEqual(1, late.Calls);
    }
}
