
using UnityEngine;

public interface IGamePieces
{
    Transform Transform { get; }
    IMovable Movement { get; }
}