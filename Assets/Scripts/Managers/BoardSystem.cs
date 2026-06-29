using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BoardSystem
{
    private SharedBoardState SharedBoard;

    public void Initialize(SharedBoardState sharedBoard)
    {
        SharedBoard = sharedBoard;
    }

    public bool TryDragUnit(PlayerState player, BoardUnitInstance unit, GamePhase phase)
    {
        if (unit.OwnerPlayerId != player.PlayerId)
        {
            return false;
        }

        if (phase != GamePhase.Preparation)
        {
            if (SharedBoard.Tiles.ContainsKey(unit.Node))
            {
                if (SharedBoard.Tiles[unit.Node].Location == BoardType.Bench)
                {
                    return false;
                }
            }
        }

        return true;
    }

    public bool TryDropUnit(PlayerState player, BoardUnitInstance unit, BoardNode destination, GamePhase phase)
    {
        if (unit.OwnerPlayerId != player.PlayerId)
        {
            return false;
        }

        if (!SharedBoard.TryGetTile(unit.Node, out BoardTileState unitTile))
            return false;

        if (!SharedBoard.TryGetTile(destination, out BoardTileState destinationTile))
            return false;

        if (destinationTile.HomePlayerId != player.PlayerId)
        {
            return false;
        }

        if (phase != GamePhase.Preparation)
        {
            if (destinationTile != null && destinationTile.Location != BoardType.Bench)
            {
                return false;
            }
        }
        else
        {
            if (destinationTile == null)
            {
                return false;
            }

            if (unitTile.Location == BoardType.Bench)
            {
                if (destinationTile.Location == BoardType.Board)
                {
                    if (player.Board.UnitsOnBoard >= player.Board.BoardCapacity)
                    {
                        return false;
                    }
                    else
                    {
                        if (player.Board.IsNodeOccupied(destination))
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (player.Board.IsNodeOccupied(destination))
                    {
                        if (player.Board.TryGetUnitAt(destination, out BoardUnitInstance other) && other != unit)
                        {
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (destinationTile.Location == BoardType.Board)
                {
                    if (player.Board.IsNodeOccupied(destination))
                    {
                        if (player.Board.TryGetUnitAt(destination, out BoardUnitInstance other) && other != unit)
                        {
                            return false;
                        }
                    }
                }
                else
                {
                    if (player.Board.IsNodeOccupied(destination))
                    {
                        if (player.Board.TryGetUnitAt(destination, out BoardUnitInstance other) && other != unit)
                        {
                            return false;
                        }
                    }
                }
            }
        }

        return true;
    }

    public void DropUnit(PlayerState player, BoardUnitInstance unit, BoardNode destination)
    {
        BoardType previousLocation = SharedBoard.GetLocationAt(unit.Node);
        BoardType newLocation = SharedBoard.GetLocationAt(destination);

        player.Board.UnitIdByNode.Remove(unit.Node);

        unit.Node = destination;

        player.Board.UnitIdByNode[destination] = unit.InstanceId;

        if (previousLocation != BoardType.Board &&
            newLocation == BoardType.Board)
        {
            player.Board.UnitsOnBoard++;
        }

        if (previousLocation == BoardType.Board &&
            newLocation != BoardType.Board)
        {
            player.Board.UnitsOnBoard--;
        }
    }

    public bool TryDropBiome(PlayerState player, BoardNode destination, BiomeType biomeType, GamePhase phase)
    {
        if (phase != GamePhase.Preparation)
        {
            return false;
        }

        BoardTileState destinationTile = SharedBoard.Tiles[destination];

        if (destinationTile.Location == BoardType.Bench)
        {
            return false;
        }
        else if (destinationTile.Biome == biomeType)
        {
            return false;
        }
        else if (player.BiomeCount == 0)
        {
            return false;
        }

        return true;
    }

    public void DropBiome(PlayerState player, BoardNode destination, BiomeType biome)
    {
        BoardTileState destinationTile = SharedBoard.Tiles[destination];
        destinationTile.Biome = biome;
        player.BiomeCount--;
    }
}
