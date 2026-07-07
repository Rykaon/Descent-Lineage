using UnityEngine;

public class NetworkConfig
{
    public static NetworkGameRole Role = NetworkGameRole.Client;

    public static string Address = "";
    public static ushort Port = 7779;

    public static bool IsServer => Role == NetworkGameRole.DedicatedServer;
    public static bool IsClient => Role == NetworkGameRole.Client;
}
