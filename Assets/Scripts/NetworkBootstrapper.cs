using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public sealed class NetworkBootstrapper : MonoBehaviour
{
    [SerializeField] private GameSessionConfig sessionConfig;
    [SerializeField] private UnityTransport transport;

    [SerializeField] private string serverAddress = "127.0.0.1";
    [SerializeField] private ushort serverPort = 7778;

    private void Start()
    {
        if (sessionConfig.Role == NetworkGameRole.Local)
        {
            Debug.Log("[NETWORK] Local mode");
            return;
        }

        transport.SetConnectionData(serverAddress, serverPort);

        if (sessionConfig.Role == NetworkGameRole.DedicatedServer)
        {
            NetworkManager.Singleton.StartServer();
            Debug.Log("[NETWORK] Started dedicated server");
            return;
        }

        if (sessionConfig.Role == NetworkGameRole.Client)
        {
            NetworkManager.Singleton.StartClient();
            Debug.Log("[NETWORK] Started client");
        }
    }

    private void OnDestroy()
    {
        ShutdownNetwork();
    }

    private void OnApplicationQuit()
    {
        ShutdownNetwork();
    }

    private void ShutdownNetwork()
    {
        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }

        Debug.Log("[NETWORK] Shutdown");

        NetworkManager.Singleton.Shutdown();
    }
}