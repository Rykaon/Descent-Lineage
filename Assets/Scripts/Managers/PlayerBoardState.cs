using System.Collections.Generic;
using System;

public class PlayerBoardState
{
    public int Width;
    public int Height;
    public int BoardCapacity;
    public int UnitsOnBoard;

    public List<BoardUnitInstance> Units = new();

    public Dictionary<string, BoardUnitInstance> UnitByInstanceId = new();
    public Dictionary<string, BoardUnitInstance> UnitByDefinitionId = new();
    public Dictionary<BoardNode, string> UnitIdByNode = new();

    public void Initialize()
    {
        Width = 9;
        Height = 4;
        BoardCapacity = 0;
        UnitsOnBoard = 0;
    }

    public bool TryGetUnitByDefinitionId(string definitionId, out BoardUnitInstance unit)
    {
        return UnitByDefinitionId.TryGetValue(definitionId, out unit);
    }

    public bool TryGetUnitByInstanceId(string instanceId, out BoardUnitInstance unit)
    {
        return UnitByInstanceId.TryGetValue(instanceId, out unit);
    }

    public bool HasUnitDefinition(string definitionId)
    {
        return UnitByDefinitionId.ContainsKey(definitionId);
    }

    public bool TryGetUnitAt(BoardNode node, out BoardUnitInstance unit)
    {
        unit = null;

        if (!UnitIdByNode.TryGetValue(node, out string instanceId))
            return false;

        return UnitByInstanceId.TryGetValue(instanceId, out unit);
    }

    public void RegisterUnit(BoardUnitInstance unit, BoardTileState tile)
    {
        UnitByInstanceId[unit.InstanceId] = unit;
        UnitByDefinitionId[unit.DefinitionId] = unit;
        UnitIdByNode[unit.Node] = unit.InstanceId;
        Units.Add(unit);

        if (tile != null)
        {
            if (tile.Location == BoardType.Board)
            {
                UnitsOnBoard++;
            }
        }
    }

    public void UnregisterUnit(BoardUnitInstance unit, BoardTileState tile)
    {
        UnitByInstanceId.Remove(unit.InstanceId);
        UnitByDefinitionId.Remove(unit.DefinitionId);
        UnitIdByNode.Remove(unit.Node);
        Units.Remove(unit);

        if (tile != null)
        {
            if (tile.Location == BoardType.Board)
            {
                UnitsOnBoard--;
            }
        }
    }

    public bool IsNodeOccupied(BoardNode node)
    {
        return UnitIdByNode.ContainsKey(node);
    }
}

public class BoardUnitInstance
{
    public string DefinitionId;
    public string InstanceId;
    public int OwnerPlayerId;
    public int OwnedCopyCount = 1;

    public BoardNode Node;

    public List<string> MutationIds;

    public BoardUnitInstance(string definitionId, int ownerPlayerId, BoardNode node)
    {
        DefinitionId = definitionId;
        InstanceId = CreateUnitInstanceId();
        OwnerPlayerId = ownerPlayerId;
        OwnedCopyCount = 1;
        Node = node;
        MutationIds = new List<string>();
    }

    private string CreateUnitInstanceId()
    {
        return Guid.NewGuid().ToString("N");
    }
}