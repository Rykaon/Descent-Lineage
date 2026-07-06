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

    public override void OnNetworkSpawn()
    {
        Debug.Log($"[NETWORK BRIDGE SPAWN] IsServer={IsServer} IsClient={IsClient} role={sessionConfig.Role}");

        if (IsServer)
        {
            snapshotBuilder = new NetworkSnapshotBuilder(gameController);

            playerRegistry.Clear();

            NetworkManager.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback += HandleClientDisconnected;

            foreach (ulong clientId in NetworkManager.ConnectedClientsIds)
            {
                RegisterClient(clientId);
            }

            if (sessionConfig.Role == NetworkGameRole.DedicatedServer)
            {
                gameController.StartGame();
                BroadcastBoardState();
                BroadcastShopState();
                BroadcastFaunaShopState();
            }
        }

        if (IsClient && !IsServer)
        {
            Debug.Log($"[BRIDGE] Create applier context={clientContext.GetInstanceID()} mirrorNull={clientContext.Mirror == null}");
            snapshotApplier = new NetworkSnapshotApplier(clientContext);
            InitializeClientInput();
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
            NetworkManager.OnClientConnectedCallback -= HandleClientConnected;
            NetworkManager.OnClientDisconnectCallback -= HandleClientDisconnected;
        }
    }

    private void OnEnable()
    {
        gameController.OnPhaseChanged += HandlePhaseChanged;
        gameController.OnBattleTicked += HandleBattleTicked;
        gameController.OnPreparationTicked += HandlePreparationTicked;
    }

    private void OnDisable()
    {
        gameController.OnPhaseChanged -= HandlePhaseChanged;
        gameController.OnBattleTicked -= HandleBattleTicked;
        gameController.OnPreparationTicked -= HandlePreparationTicked;
    }

    private void HandleClientConnected(ulong clientId)
    {
        RegisterClient(clientId);
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        playerRegistry.UnregisterClient(clientId);
    }

    private void InitializeClientInput()
    {
        if (sessionConfig.Role != NetworkGameRole.Client)
        {
            return;
        }

        IGameCommandSender sender = new NetworkGameCommandSender(this);

        if (sessionConfig.LocalPlayerId == 0)
        {
            snapshotApplier.Mirror.LocalPlayerId = sessionConfig.LocalPlayerId;
            snapshotApplier.Mirror.OpponentPlayerId = 1;
        }
        else
        {
            snapshotApplier.Mirror.LocalPlayerId = sessionConfig.LocalPlayerId;
            snapshotApplier.Mirror.OpponentPlayerId = 0;
        }

        localInput.Initialize(new LocalPlayerContext(sessionConfig.LocalPlayerId), sender, snapshotApplier.Mirror);

        Debug.Log("[NETWORK] Client input initialized");
    }

    private void RegisterClient(ulong clientId)
    {
        if (!playerRegistry.TryRegisterClient(clientId, out int playerId))
        {
            Debug.LogWarning($"[NETWORK] Client rejected, match already full. clientId={clientId}");
            NetworkManager.DisconnectClient(clientId);
            return;
        }

        Debug.Log($"[NETWORK] Registered clientId={clientId} as playerId={playerId}");

        SendInitialStateToClient(clientId);
    }

    private void SendInitialStateToClient(ulong clientId)
    {
        SendInitialStateRpc(
            snapshotBuilder.BuildBoardStateSnapshot(),
            snapshotBuilder.BuildPlayerStateSnapshot(),
            snapshotBuilder.BuildShopStateSnapshot(),
            snapshotBuilder.BuildFaunaShopStateSnapshot(),
            snapshotBuilder.BuildFossilStateSnapshot(),
            new GamePhaseSnapshot
            {
                Phase = gameController.State.Phase
            },
            RpcTarget.Single(clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendInitialStateRpc(BoardStateSnapshot board, PlayerStateSnapshot[] players, ShopStateSnapshot shop, FaunaShopStateSnapshot faunaShop, FossilStateSnapshot[] fossils, GamePhaseSnapshot phase, RpcParams rpcParams = default)
    {
        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyPhaseState(phase);
        snapshotApplier.ApplyPlayerStates(players);
        snapshotApplier.ApplyBoardState(board);
        snapshotApplier.ApplyShopState(shop);
        snapshotApplier.ApplyFaunaShopState(faunaShop);
        snapshotApplier.ApplyFossilStates(fossils);

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
        }

        if (phase == GamePhase.Preparation)
        {
            BroadcastBoardState();
            BroadcastPlayerState();
            BroadcastShopState();
        }

        if (phase == GamePhase.PreBattle)
        {
            localInput.HandlePhaseChanged(phase);
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
                break;

            case GameCommandType.SellUnit:
                BroadcastBoardState();
                BroadcastPlayerState();
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

        Debug.Log($"[SERVER] BroadcastBattleFrame units={snapshot.Units.Length}");

        ReceiveBattleFrameRpc(snapshot);
    }

    private void BroadcastBattleEvents()
    {
        BattleEventsSnapshot snapshot = snapshotBuilder.BuildBattleEventsSnapshot();

        if (snapshot.DamageEvents.Length == 0)
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

        Debug.Log($"[NETWORK] Board snapshot received. units={snapshot.Units.Length}, tiles={snapshot.Tiles.Length}");

        snapshotApplier.ApplyBoardState(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceivePlayerStateRpc(PlayerStateSnapshot[] snapshots)
    {
        if (IsServer)
        {
            return;
        }

        Debug.Log($"[NETWORK] Player snapshot received. players={snapshots.Length}");

        snapshotApplier.ApplyPlayerStates(snapshots);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveShopStateRpc(ShopStateSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        Debug.Log($"[NETWORK] Shop snapshot received. slots={snapshot.Slots.Length}");

        snapshotApplier.ApplyShopState(snapshot);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void ReceiveFaunaShopStateRpc(FaunaShopStateSnapshot snapshot)
    {
        if (IsServer)
        {
            return;
        }

        Debug.Log($"[NETWORK] Shop snapshot received. slots={snapshot.Slots.Length}");

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
        Debug.Log($"[CLIENT RPC] BattleFrame received units={snapshot.Units.Length}");

        if (IsServer)
        {
            return;
        }

        snapshotApplier.ApplyBattleFrame(snapshot);
    }
}