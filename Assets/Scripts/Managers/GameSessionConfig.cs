using UnityEngine;

public sealed class GameSessionConfig : MonoBehaviour
{
    [field: SerializeField] public NetworkGameRole Role { get; private set; } = NetworkGameRole.Local;
    [field: SerializeField] public int LocalPlayerId { get; private set; } = 0;
}