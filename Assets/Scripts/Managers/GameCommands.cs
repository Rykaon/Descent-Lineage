using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Collections;
using Unity.Netcode;
using UnityEngine;

public enum GameCommandType
{
    None,

    BuyShopUnit,
    BuyShopFauna,
    DragBoardUnit,
    DropBoardUnit,
    SellUnit,
    SellBiome,
    DropBiomeTile,
    RerollShop,

    SetReady
}

[Serializable]
public struct GameCommand : INetworkSerializable
{
    public int PlayerId;
    public GameCommandType Type;

    public int ShopSlotIndex;

    public FixedString64Bytes UnitInstanceId;
    public FixedString64Bytes UnitDefinitionId;

    public BoardNode FromNode;
    public BoardNode ToNode;

    public BiomeType BiomeType;

    public bool Ready;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref Type);
        serializer.SerializeValue(ref ShopSlotIndex);
        serializer.SerializeValue(ref UnitInstanceId);
        serializer.SerializeValue(ref UnitDefinitionId);
        serializer.SerializeValue(ref FromNode);
        serializer.SerializeValue(ref ToNode);
        serializer.SerializeValue(ref BiomeType);
        serializer.SerializeValue(ref Ready);
    }
}

public struct GameCommandResult
{
    public bool Success;
    public string Reason;

    public static GameCommandResult Ok()
    {
        return new GameCommandResult { Success = true };
    }

    public static GameCommandResult Fail(string reason)
    {
        return new GameCommandResult
        {
            Success = false,
            Reason = reason
        };
    }
}


public sealed class LocalGameCommandSender : IGameCommandSender
{
    private readonly GameController gameController;

    public LocalGameCommandSender(GameController gameController)
    {
        this.gameController = gameController;
    }

    public GameCommandResult SubmitCommand(GameCommand command)
    {
        return gameController.ApplyCommand(command);
    }
}

public sealed class NetworkGameCommandSender : IGameCommandSender
{
    private readonly NetworkGameBridge bridge;

    public NetworkGameCommandSender(NetworkGameBridge bridge)
    {
        this.bridge = bridge;
    }

    public GameCommandResult SubmitCommand(GameCommand command)
    {
        Debug.Log($"[CLIENT COMMAND] Submit {command.Type}");
        bridge.SubmitCommandToServer(command);

        return GameCommandResult.Ok();
    }
}

public sealed class NetworkPlayerRegistry
{
    private readonly Dictionary<ulong, int> playerIdByClientId = new();

    public void Clear()
    {
        playerIdByClientId.Clear();
    }

    public bool TryRegisterClient(ulong clientId, out int playerId)
    {
        if (playerIdByClientId.TryGetValue(clientId, out playerId))
        {
            return true;
        }

        if (playerIdByClientId.Count >= 2)
        {
            playerId = -1;
            return false;
        }

        playerId = playerIdByClientId.Count;
        playerIdByClientId[clientId] = playerId;

        return true;
    }

    public bool TryGetPlayerId(ulong clientId, out int playerId)
    {
        return playerIdByClientId.TryGetValue(clientId, out playerId);
    }

    public void UnregisterClient(ulong clientId)
    {
        playerIdByClientId.Remove(clientId);
    }
}