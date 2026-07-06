using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Unity.Netcode;
using System;

public class SharedBoardState
{
    public int Width;
    public int Height;

    public float TileSpacing = 1.1f;
    public Vector2 Origin = Vector2.zero;

    public Dictionary<BoardNode, BoardTileState> Tiles = new();

    public void Initialize()
    {
        Width = 9;
        Height = 10;

        Tiles.Clear();

        for (int y = 0; y < BoardGeometry.Height; y++)
        {
            int rowWidth = BoardGeometry.GetRowWidth(y);

            for (int x = 0; x < rowWidth; x++)
            {
                var node = new BoardNode(x, y);

                bool isBenchRow = y == 0 || y == BoardGeometry.Height - 1;

                Tiles[node] = new BoardTileState
                {
                    Node = node,
                    HomePlayerId = y < BoardGeometry.Height / 2 ? 0 : 1,
                    Biome = BiomeType.None,
                    Location = isBenchRow ? BoardType.Bench : BoardType.Board,
                };
            }
        }
    }

    public bool TryGetTile(BoardNode node, out BoardTileState tile)
    {
        return Tiles.TryGetValue(node, out tile);
    }

    public BiomeType GetBiomeTypeAt(BoardNode node)
    {
        return Tiles.TryGetValue(node, out var tile)
            ? tile.Biome
            : BiomeType.None;
    }

    public BoardType GetLocationAt(BoardNode node)
    {
        return Tiles.TryGetValue(node, out var tile)
            ? tile.Location
            : BoardType.None;
    }

    public IEnumerable<BoardTileState> GetTilesForPlayerOrdered(
    PlayerState player,
    BoardType location)
    {
        return Tiles.Values
            .Where(t => t.HomePlayerId == player.PlayerId && t.Location == location)
            .OrderBy(t => player.PlayerId == 0 ? t.Node.Y : -t.Node.Y)
            .ThenBy(t => t.Node.X);
    }

    public bool TryGetFirstFreeTile(
    PlayerState player,
    BoardType location,
    out BoardTileState result)
    {
        result = null;

        foreach (BoardTileState tile in GetTilesForPlayerOrdered(player, location))
        {
            if (player.Board.IsNodeOccupied(tile.Node))
                continue;

            result = tile;
            return true;
        }

        return false;
    }
}

public class BoardTileState
{
    public int HomePlayerId;
    public BoardNode Node;
    public BoardType Location;
    public BiomeType Biome;
}

public struct BoardNode : IEquatable<BoardNode>, INetworkSerializable
{
    public int X { get; private set; }
    public int Y { get; private set; }

    public BoardNode(int x, int y)
    {
        X = x;
        Y = y;
    }

    public bool Equals(BoardNode other)
    {
        return X == other.X && Y == other.Y;
    }

    public override bool Equals(object obj)
    {
        return obj is BoardNode other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(X, Y);
    }

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        int x = X;
        int y = Y;

        serializer.SerializeValue(ref x);
        serializer.SerializeValue(ref y);

        if (serializer.IsReader)
        {
            X = x;
            Y = y;
        }
    }
}

public static class BoardGeometry
{
    public const int Width = 9;
    public const int Height = 10;
    public const float TileSpacing = 1.1f;
    public const int BoardToBattleSpacing = 3;

    public static float BattleHexSize => TileSpacing / BoardToBattleSpacing;

    public static readonly Vector2 Origin = Vector2.zero;

    public static int GetRowWidth(int y)
    {
        if (y == 0 || y == Height - 1)
        {
            return 9;
        }

        return y % 2 == 0 ? 9 : 8;
    }

    public static bool IsInside(BoardNode node)
    {
        if (node.Y < 0 || node.Y >= Height)
        {
            return false;
        }

        return node.X >= 0 && node.X < GetRowWidth(node.Y);
    }

    public static Vector2 NodeToWorld2D(BoardNode node)
    {
        float xOffset = node.Y % 2 == 0 ? 0f : TileSpacing * 0.5f;

        float x = node.X * TileSpacing + xOffset;
        float y = node.Y * TileSpacing * 0.8660254f;

        return Origin + new Vector2(x, y);
    }

    public static Vector3 NodeToWorld(BoardNode node)
    {
        Vector2 p = NodeToWorld2D(node);
        return new Vector3(p.x, 0f, p.y);
    }

    public static Vector2 HexToWorld2D(BattleHexCoord hex)
    {
        float hexSize = BattleHexSize;

        float xOffset = hex.R % 2 == 0 ? 0f : hexSize * 0.5f;

        float x = hex.Q * hexSize + xOffset;
        float y = hex.R * hexSize * 0.8660254f;

        return new Vector2(x, y);
    }

    public static Vector3 HexToWorld(BattleHexCoord hex)
    {
        Vector2 position = HexToWorld2D(hex);
        return new Vector3(position.x, 0f, position.y);
    }
}