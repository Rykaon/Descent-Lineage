using System.Collections.Generic;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public sealed class NetworkGameBridge : NetworkBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private ClientGameContext clientContext;
    [SerializeField] private GameSessionConfig sessionConfig;
    [SerializeField] private PlayerInputController localInput;

    private NetworkSnapshotBuilder snapshotBuilder;
    private NetworkSnapshotApplier snapshotApplier;

    private readonly NetworkPlayerRegistry playerRegistry = new();

    private int battleSnapshotTickCounter;
    private const int BattleSnapshotEveryTicks = 1;

    private const int RequiredPlayers = 2;
    private readonly HashSet<ulong> readyClients = new();
    private bool gameStarted;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            gameController = FindFirstObjectByType<GameController>();
            sessionConfig = FindFirstObjectByType<GameSessionConfig>();
        }

        if (IsClient && !IsServer)
        {
            sessionConfig = FindFirstObjectByType<GameSessionConfig>();
            clientContext = FindFirstObjectByType<ClientGameContext>();
            localInput = FindFirstObjectByType<PlayerInputController>();
        }

        Debug.Log($"[NETWORK BRIDGE SPAWN] IsServer={IsServer} IsClient={IsClient} role={sessionConfig.Role}");

        if (IsServer)
        {
            gameController = FindFirstObjectByType<GameController>();
            sessionConfig = FindFirstObjectByType<GameSessionConfig>();

            gameController.OnPhaseChanged += HandlePhaseChanged;
            gameController.OnBattleTicked += HandleBattleTicked;
            gameController.OnPreparationTicked += HandlePreparationTicked;

            snapshotBuilder = new NetworkSnapshotBuilder(gameController);

            playerRegistry.Clear();

            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                RegisterClient(clientId);
            }

            Debug.Log("[SERVER] Dedicated server ready. Waiting for clients.");
        }

        if (IsClient && !IsServer)
        {
            Debug.Log($"[BRIDGE] Create applier context={clientContext.GetInstanceID()} mirrorNull={clientContext.Mirror == null}");
            snapshotApplier = new NetworkSnapshotApplier(clientContext);
        }
    }

    public override void OnNetworkDespawn()
    {
        if (NetworkManager == null)
        {
            return;
        }

        if (IsServer)
        {
            if (gameController != null)
            {
                gameController.OnPhaseChanged -= HandlePhaseChanged;
                gameController.OnBattleTicked -= HandleBattleTicked;
                gameController.OnPreparationTicked -= HandlePreparationTicked;
            }

            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"[BRIDGE] HandleClientConnected clientId={clientId}");
        RegisterClient(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        readyClients.Remove(clientId);
        playerRegistry.UnregisterClient(clientId);

        Debug.Log($"[SERVER] Client disconnected clientId={clientId}");

        TryShutdownServerAfterFinishedGame();
    }

    private void TryStartGameWhenReady()
    {
        if (gameStarted)
        {
            return;
        }

        if (!IsServer)
        {
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count < RequiredPlayers)
        {
            Debug.Log($"[SERVER] Waiting clients connected {NetworkManager.Singleton.ConnectedClientsIds.Count}/{RequiredPlayers}");
            return;
        }

        if (readyClients.Count < RequiredPlayers)
        {
            Debug.Log($"[SERVER] Waiting clients ready {readyClients.Count}/{RequiredPlayers}");
            return;
        }

        gameStarted = true;

        Debug.Log("[SERVER] All clients ready. Starting game.");

        gameController.StartGame();
    }

    private void TryShutdownServerAfterFinishedGame()
    {
        if (!IsServer)
        {
            return;
        }

        if (gameController == null || !gameController.IsGameFinished)
        {
            Debug.Log("[SERVER] Client disconnected but game is not finished. Server stays alive.");
            return;
        }

        if (NetworkManager.Singleton.ConnectedClientsIds.Count > 0)
        {
            return;
        }

        Debug.Log("[SERVER] Game finished and all clients left. Shutting down server.");

        Application.Quit();
    }

    private void RegisterClient(ulong clientId)
    {
        if (!playerRegistry.TryRegisterClient(clientId, out int playerId))
        {
            Debug.LogWarning($"[NETWORK] Client rejected, match already full. clientId={clientId}");
            NetworkManager.DisconnectClient(clientId);
            return;
        }

        readyClients.Remove(clientId);

        Debug.Log($"[NETWORK] Registered clientId={clientId} as playerId={playerId}");

        SendInitialStateToClient(clientId);
    }

    private void SendInitialStateToClient(ulong clientId)
    {
        if (gameController == null)
        {
            return;
        }

        playerRegistry.TryGetPlayerId(clientId, out int playerId);

        SendInitialStateRpc(
            playerId,
            snapshotBuilder.BuildBoardStateSnapshot(),
            snapshotBuilder.BuildPlayerStateSnapshot(),
            snapshotBuilder.BuildShopStateSnapshot(),
            snapshotBuilder.BuildFaunaShopStateSnapshot(),
            snapshotBuilder.BuildFossilStateSnapshot(),
            snapshotBuilder.BuildCladeStateSnapshot(),
            new GamePhaseSnapshot
            {
                Phase = gameController.State.Phase
            },
            RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendInitialStateRpc(int localPlayerId, BoardStateSnapshot board, PlayerStateSnapshot[] players, ShopStateSnapshot shop, FaunaShopStateSnapshot faunaShop, FossilStateSnapshot[] fossils, CladeStateSnapshot[] clades, GamePhaseSnapshot phase, RpcParams rpcParams = default)
    {
        if (IsClient && !IsServer)
        {
            int opponentPlayerId = localPlayerId == 0 ? 1 : 0;

            snapshotApplier.Mirror.LocalPlayerId = localPlayerId;
            snapshotApplier.Mirror.OpponentPlayerId = opponentPlayerId;

            localInput.Initialize(new LocalPlayerContext(localPlayerId), new NetworkGameCommandSender(this), snapshotApplier.Mirror);

            snapshotApplier.ApplyPhaseState(phase);
            snapshotApplier.ApplyPlayerStates(players);
            snapshotApplier.ApplyBoardState(board);
            snapshotApplier.ApplyShopState(shop);
            snapshotApplier.ApplyFaunaShopState(faunaShop);
            snapshotApplier.ApplyFossilStates(fossils);
            snapshotApplier.ApplyCladeStates(clades);

            clientContext.NotifyInitialStateReady();
            ClientInitialLoadReadyRpc();
        }
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void ClientInitialLoadReadyRpc(RpcParams rpcParams = default)
    {
        ulong clientId = rpcParams.Receive.SenderClientId;

        if (!playerRegistry.TryGetPlayerId(clientId, out int playerId))
        {
            Debug.LogWarning($"[SERVER] Ready rejected from unregistered clientId={clientId}");
            return;
        }

        readyClients.Add(clientId);

        Debug.Log($"[SERVER] Client ready clientId={clientId} playerId={playerId} ready={readyClients.Count}/{RequiredPlayers}");

        TryStartGameWhenReady();
    }

    public void SubmitCommandToServer(GameCommand command)
    {
        SubmitCommandRpc(command);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    private void SubmitCommandRpc(GameCommand command, RpcParams rpcParams = default)
    {
        ulong senderClientId = rpcParams.Receive.SenderClientId;

        if (!playerRegistry.TryGetPlayerId(senderClientId, out int playerId))
        {
            Debug.LogWarning($"[NETWORK] Command rejected from unregistered clientId={senderClientId}");
            return;
        }

        Debug.Log($"[SERVER COMMAND] client={senderClientId} player={playerId} type={command.Type}");

        command.PlayerId = playerId;

        GameCommandResult result = gameController.ApplyCommand(command);

        Debug.Log($"[SERVER COMMAND RESULT] type={command.Type} success={result.Success} reason={result.Reason}");

        if (result.Success)
        {
            BroadcastAfterCommand(command);
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        if (!IsServer)
        {
            return;
        }

        BroadcastPhaseState();

        if (phase == GamePhase.Setup)
        {
            BroadcastBoardState();
            BroadcastPlayerState();
            BroadcastShopState();
            BroadcastFaunaShopState();
            BroadcastFossilState();
        }

        if (phase == GamePhase.Preparation)
        {
            BroadcastBoardState();
            BroadcastPlayerState();
            BroadcastShopState();
            BroadcastFaunaShopState();
            BroadcastFossilState();
        }

        if (phase == GamePhase.Battle)
        {
            battleSnapshotTickCounter = 0;
            BroadcastBattleInit();
            BroadcastBattleFrame();
        }

        if (phase == GamePhase.PostBattle)
        {
            BroadcastBoardState();
            BroadcastPlayerState();
        }
    }

    private void HandlePreparationTicked(int remainingTime)
    {
        BroadcastTimer(remainingTime);
    }

    private void HandleBattleTicked(BattleState battleState)
    {
        if (!CanSendRpc())
        {
            return;
        }

        BroadcastBattleEvents();

        battleSnapshotTickCounter++;

        if (battleSnapshotTickCounter >= BattleSnapshotEveryTicks)
        {
            battleSnapshotTickCounter = 0;
            BroadcastBattleFrame();
        }

        gameController.Battle.EventBuffer.Clear();
    }

    private bool CanSendRpc()
    {
        return NetworkManager != null && NetworkManager.IsListening && IsSpawned && IsServer;
    }

    private void BroadcastAfterCommand(GameCommand command)
    {
        switch (command.Type)
        {
            case GameCommandType.BuyShopUnit:
                BroadcastBoardState();
                BroadcastPlayerState();
                BroadcastShopState();
                BroadcastCladeState();
                break;

            case GameCommandType.BuyShopFauna:
                BroadcastPlayerState();
                BroadcastFaunaShopState();
                BroadcastFossilState();
                break;

            case GameCommandType.RerollShop:
                BroadcastPlayerState();
                BroadcastShopState();
                BroadcastFaunaShopState();
                break;

            case GameCommandType.DropBoardUnit:
                BroadcastBoardState();
                BroadcastCladeState();
                break;

            case GameCommandType.SellUnit:
                BroadcastBoardState();
                BroadcastPlayerState();
                BroadcastCladeState();
                break;

            case GameCommandType.DropBiomeTile:
                BroadcastBoardState();
                BroadcastPlayerState();
                break;

            case GameCommandType.SellBiome:
                BroadcastPlayerState();
                break;
        }
    }

    private void BroadcastTimer(int remaingTime)
    {
        ReceiveTimerRpc(remaingTime);
    }

    private void BroadcastBoardState()
    {
        ReceiveBoardStateRpc(snapshotBuilder.BuildBoardStateSnapshot());
    }

    private void BroadcastCladeState()
    {
        ReceiveCladeStateRpc(snapshotBuilder.BuildCladeStateSnapshot());
    }

    private void BroadcastPlayerState()
    {
        ReceivePlayerStateRpc(snapshotBuilder.BuildPlayerStateSnapshot());
    }

    private void BroadcastShopState()
    {
        ReceiveShopStateRpc(snapshotBuilder.BuildShopStateSnapshot());
    }

    private void BroadcastFaunaShopState()
    {
        ReceiveFaunaShopStateRpc(snapshotBuilder.BuildFaunaShopStateSnapshot());
    }

    private void BroadcastPhaseState()
    {
        ReceivePhaseStateRpc(new GamePhaseSnapshot
        {
            Phase = gameController.State.Phase
        });
    }

    private void BroadcastFossilState()
    {
        if (!CanSendRpc())
        {
            return;
        }

        var snapshot = snapshotBuilder.BuildFossilStateSnapshot();
        ReceiveFossilStateRpc(snapshot);
    }

    private void BroadcastBattleInit()
    {
        if (!CanSendRpc())
        {
            return;
        }

        BattleInitSnapshot snapshot = snapshotBuilder.BuildBattleInitSnapshot();
        ReceiveBattleInitRpc(snapshot);
    }

    private void BroadcastBattleFrame()
    {
        if (!CanSendRpc())
        {
            return;
        }

        BattleFrameSnapshot snapshot = snapshotBuilder.BuildBattleFrameSnapshot();

        ReceiveBattleFrameRpc(snapshot);
    }

    private void BroadcastBattleEvents()
    {
        BattleEventsSnapshot snapshot = snapshotBuilder.BuildBattleEventsSnapshot();

        if ((snapshot.DamageEvents == null || snapshot.DamageEvents.Length == 0) &&
            (snapshot.ManaEvents == null || snapshot.ManaEvents.Length == 0) &&
            (snapshot.HealEvents == null || snapshot.HealEvents.Length == 0))
        {
            return;
        }

        ReceiveBattleEventsRpc(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveTimerRpc(int remaingTime)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyTimer(remaingTime);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveBattleEventsRpc(BattleEventsSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyBattleEvents(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceivePhaseStateRpc(GamePhaseSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyPhaseState(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveBoardStateRpc(BoardStateSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyBoardState(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveCladeStateRpc(CladeStateSnapshot[] snapshots)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyCladeStates(snapshots);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceivePlayerStateRpc(PlayerStateSnapshot[] snapshots)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyPlayerStates(snapshots);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveShopStateRpc(ShopStateSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyShopState(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveFaunaShopStateRpc(FaunaShopStateSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyFaunaShopState(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveFossilStateRpc(FossilStateSnapshot[] snapshots)
    {
        snapshotApplier.ApplyFossilStates(snapshots);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveBattleInitRpc(BattleInitSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyBattleInit(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveBattleFrameRpc(BattleFrameSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyBattleFrame(snapshot);
    }
}