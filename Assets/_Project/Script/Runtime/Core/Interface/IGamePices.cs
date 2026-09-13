
using UnityEngine;

public interface IGamePices
{
    Transform Transform { get; }
    IMovable Movement { get; }
}