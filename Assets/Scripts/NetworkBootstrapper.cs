using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using System.Collections;

public sealed class NetworkBootstrapper : MonoBehaviour
{
    [SerializeField] private GameSessionConfig sessionConfig;
    [SerializeField] private UnityTransport transport;
    [SerializeField] private NetworkObject networkSessionRootPrefab;

    [Header("Fallback values")]
    [SerializeField] private string fallbackServerAddress = "127.0.0.1";
    [SerializeField] private ushort fallbackServerPort = 7777;

    private bool hasShutdown;

    private void Start()
    {
        CommandLineArgs.Apply();

        NetworkGameRole role = ResolveRole();
        string address = ResolveAddress();
        ushort port = ResolvePort();


        if (role == NetworkGameRole.Local)
        {
            return;
        }

        if (role == NetworkGameRole.DedicatedServer)
        {
            transport.SetConnectionData("127.0.0.1", port, "0.0.0.0");
        }
        else
        {
            transport.SetConnectionData(address, port, "0.0.0.0");
        }

        if (role == NetworkGameRole.DedicatedServer)
        {
            NetworkManager.Singleton.ConnectionApprovalCallback += (request, response) =>
            {
                response.Approved = true;
                response.CreatePlayerObject = false;
                response.Pending = false;
            };

            bool started = NetworkManager.Singleton.StartServer();

            if (started)
            {
                NetworkObject sessionRoot = Instantiate(networkSessionRootPrefab);

                if (sessionRoot == null)
                {
                    return;
                }

                sessionRoot.Spawn();
            }

            Debug.Log($"[SERVER BOOT] StartServer result={started} port={port}");
            return;
        }

        if (role == NetworkGameRole.Client)
        {
            bool started = NetworkManager.Singleton.StartClient();

            Debug.Log($"[CLIENT BOOT] StartClient result={started} address={address} port={port}");
        }
    }

    private NetworkGameRole ResolveRole()
    {
        if (NetworkConfig.Role != NetworkGameRole.Client)
        {
            return NetworkConfig.Role;
        }

        if (sessionConfig != null)
        {
            return sessionConfig.Role;
        }

        return NetworkConfig.Role;
    }

    private string ResolveAddress()
    {
        if (!string.IsNullOrWhiteSpace(NetworkConfig.Address))
        {
            return NetworkConfig.Address;
        }

        return fallbackServerAddress;
    }

    private ushort ResolvePort()
    {
        if (NetworkConfig.Port != 0)
        {
            return NetworkConfig.Port;
        }

        return fallbackServerPort;
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
        if (hasShutdown)
        {
            return;
        }

        if (NetworkManager.Singleton == null)
        {
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            return;
        }

        hasShutdown = true;

        Debug.Log("[NETWORK] Shutdown");
        NetworkManager.Singleton.Shutdown();
    }
}