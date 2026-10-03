using System.Collections.Generic;
using NUnit.Framework;
using R3;
using UnityEngine;
using UnityEngine.TestTools;

public class GameSessionTests
{
    private GameSession _session;

    [SetUp]
    public void SetUp() => _session = new GameSession();

    [TearDown]
    public void TearDown() => _session.Dispose();

    [Test]
    public void StartsInLoading()
    {
        Assert.AreEqual(GameState.Loading, _session.CurrentState);
        Assert.IsFalse(_session.IsPlaying);
    }

    [Test]
    public void HappyPath_Loading_Playing_Paused_Playing_Won()
    {
        Assert.IsTrue(_session.StartPlaying());
        Assert.IsTrue(_session.Pause());
        Assert.IsTrue(_session.Resume());
        Assert.IsTrue(_session.Win());
        Assert.AreEqual(GameState.Won, _session.CurrentState);
        Assert.IsTrue(_session.IsFinished);
    }

    [Test]
    public void CannotWinOrLose_WhenNotPlaying()
    {
        Assert.IsFalse(_session.Win());
        Assert.IsFalse(_session.Lose());
        Assert.AreEqual(GameState.Loading, _session.CurrentState);
    }

    [Test]
    public void CannotLose_AfterWin()
    {
        _session.StartPlaying();
        _session.Win();
        LogAssert.Expect(LogType.Warning, new System.Text.RegularExpressions.Regex("Invalid transition"));
        Assert.IsFalse(_session.Lose());
        Assert.AreEqual(GameState.Won, _session.CurrentState);
    }

    [Test]
    public void Reload_ReturnsToLoading_FromAnyState()
    {
        _session.StartPlaying();
        _session.Lose();
        _session.Reload();
        Assert.AreEqual(GameState.Loading, _session.CurrentState);
        Assert.IsTrue(_session.StartPlaying());
    }

    [Test]
    public void State_PublishesChanges()
    {
        var received = new List<GameState>();
        using (_session.State.Subscribe(received.Add))
        {
            _session.StartPlaying();
            _session.Pause();
        }

        CollectionAssert.AreEqual(new[] { GameState.Loading, GameState.Playing, GameState.Paused }, received);
    }
}
