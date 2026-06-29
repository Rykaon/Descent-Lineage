using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        for (int y = 0; y < Height; y++)
        {
            for (int x = 0; x < Width; x++)
            {
                var node = new BoardNode(x, y);

                bool isBenchRow =
                    y == 0 ||
                    y == Height - 1;

                Tiles[node] = new BoardTileState
                {
                    Node = node,
                    HomePlayerId = y < Height / 2 ? 0 : 1,
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

    public int GetRowWidth(int y)
    {
        if (y == 0)
        {
            return 9;
        }

        if (y == 9)
        {
            return 9;
        }

        return y % 2 == 0 ? 9 : 8;
    }

    public bool IsInside(BoardNode node)
    {
        if (node.Y < 0 || node.Y >= Height)
        {
            return false;
        }

        return node.X >= 0 && node.X < GetRowWidth(node.Y);
    }

    public Vector2 NodeToWorld2D(BoardNode node)
    {
        float xOffset = node.Y % 2 == 0 ? 0f : TileSpacing * 0.5f;

        float x = node.X * TileSpacing + xOffset;
        float y = node.Y * TileSpacing * 0.8660254f;

        return Origin + new Vector2(x, y);
    }

    public Vector3 NodeToWorld(BoardNode node)
    {
        Vector2 p = NodeToWorld2D(node);
        return new Vector3(p.x, 0f, p.y);
    }
}

public class BoardTileState
{
    public int HomePlayerId;
    public BoardNode Node;
    public BoardType Location;
    public BiomeType Biome;
}

public readonly struct BoardNode : System.IEquatable<BoardNode>
{
    public readonly int X;
    public readonly int Y;

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
        return System.HashCode.Combine(X, Y);
    }
}