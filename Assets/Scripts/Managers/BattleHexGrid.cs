using System.Collections.Generic;
using UnityEngine;

public sealed class BattleHexGrid
{
    public readonly int Width;
    public readonly int Height;
    public readonly float HexSize;

    private readonly SharedBoardState boardState;

    private const int BoardToBattleSpacing = 3;

    private readonly Dictionary<BattleHexCoord, BoardNode> boardNodeByBattleHex = new();

    private static readonly BattleHexCoord[] EvenRowNeighborOffsets =
    {
        new(+1, 0),
        new(-1, 0),
        new(0, -1),
        new(-1, -1),
        new(0, +1),
        new(-1, +1),
    };

    private static readonly BattleHexCoord[] OddRowNeighborOffsets =
    {
        new(+1, 0),
        new(-1, 0),
        new(+1, -1),
        new(0, -1),
        new(+1, +1),
        new(0, +1),
    };

    public BattleHexGrid(SharedBoardState boardState)
    {
        this.boardState = boardState;

        Width = BoardGeometry.Width * BoardToBattleSpacing;
        Height = BoardGeometry.Height * BoardToBattleSpacing;
        HexSize = BoardGeometry.TileSpacing / BoardToBattleSpacing;

        BuildBattleHexToBoardNodeLookup();
    }

    private void BuildBattleHexToBoardNodeLookup()
    {
        boardNodeByBattleHex.Clear();

        foreach (var pair in boardState.Tiles)
        {
            BoardTileState tile = pair.Value;
            BattleHexCoord center = BoardNodeToBattleCenter(tile.Node);

            int radius = BoardToBattleSpacing;

            for (int q = center.Q - radius; q <= center.Q + radius; q++)
            {
                for (int r = center.R - radius; r <= center.R + radius; r++)
                {
                    BattleHexCoord hex = new BattleHexCoord(q, r);

                    if (!IsInside(hex))
                        continue;

                    int distance = Distance(center, hex);

                    if (distance > radius)
                        continue;

                    if (!boardNodeByBattleHex.ContainsKey(hex))
                        boardNodeByBattleHex[hex] = tile.Node;
                }
            }
        }
    }

    public bool IsInside(BattleHexCoord hex)
    {
        return hex.Q >= 0 && hex.R >= 0 && hex.Q < Width && hex.R < Height;
    }

    public IEnumerable<BattleHexCoord> GetNeighbors(BattleHexCoord hex)
    {
        BattleHexCoord[] offsets =
            hex.R % 2 == 0
                ? EvenRowNeighborOffsets
                : OddRowNeighborOffsets;

        foreach (var offset in offsets)
        {
            BattleHexCoord neighbor = new(hex.Q + offset.Q, hex.R + offset.R);

            if (IsInside(neighbor))
            {
                yield return neighbor;
            }
        }
    }

    public int Distance(BattleHexCoord a, BattleHexCoord b)
    {
        BattleHexCoord axialA = OffsetToAxial(a);
        BattleHexCoord axialB = OffsetToAxial(b);

        return BattleHexCoord.Distance(axialA, axialB);
    }

    private BattleHexCoord OffsetToAxial(BattleHexCoord offset)
    {
        int q = offset.Q - (offset.R - (offset.R & 1)) / 2;
        int r = offset.R;

        return new BattleHexCoord(q, r);
    }

    public List<BattleHexCoord> GetUnitFootprint(BattleHexCoord center)
    {
        List<BattleHexCoord> result = new(7)
    {
        center
    };

        BattleHexCoord[] offsets =
            center.R % 2 == 0
                ? EvenRowNeighborOffsets
                : OddRowNeighborOffsets;

        foreach (var offset in offsets)
        {
            result.Add(new BattleHexCoord(
                center.Q + offset.Q,
                center.R + offset.R));
        }

        return result;
    }

    public bool CanOccupy(BattleHexCoord center, BattleUnitInstance requester, BattleHexOccupation occupation)
    {
        if (!IsBattleWalkable(center))
        {
            return false;
        }

        foreach (BattleHexCoord hex in GetUnitFootprint(center))
        {
            if (!IsInside(hex))
            {
                return false;
            }

            BattleUnitInstance occupant = occupation.GetOccupant(hex);

            if (occupant != null && occupant != requester && !occupant.IsDead)
            {
                return false;
            }
        }

        return true;
    }

    public bool CanTraverse(BattleHexCoord center, BattleUnitInstance requester, BattleHexOccupation occupation)
    {
        if (!IsBattleWalkable(center))
        {
            return false;
        }

        BattleUnitInstance centerOccupant = occupation.GetCenterOccupant(center);

        if (centerOccupant != null && centerOccupant != requester && !centerOccupant.IsDead)
        {
            return false;
        }

        return true;
    }

    public Vector2 HexToWorld(BattleHexCoord hex)
    {
        float xOffset = hex.R % 2 == 0 ? 0f : HexSize * 0.5f;

        float x = hex.Q * HexSize + xOffset;

        float y = hex.R * HexSize * 0.8660254f;

        return new Vector2(x, y);
    }

    public BattleHexCoord BoardNodeToBattleCenter(BoardNode node)
    {
        BattleHexCoord boardOffset = new BattleHexCoord(node.X, node.Y);

        BattleHexCoord boardAxial = OffsetToAxial(boardOffset);

        BattleHexCoord battleAxial = new BattleHexCoord(boardAxial.Q * BoardToBattleSpacing, boardAxial.R * BoardToBattleSpacing);

        return AxialToOffset(battleAxial);
    }

    private BattleHexCoord AxialToOffset(BattleHexCoord axial)
    {
        int q = axial.Q + (axial.R - (axial.R & 1)) / 2;
        int r = axial.R;

        return new BattleHexCoord(q, r);
    }

    public BoardNode BattleHexToBoardNode(BattleHexCoord hex)
    {
        return new BoardNode(Mathf.RoundToInt(hex.Q / 3f), Mathf.RoundToInt(hex.R / 3f));
    }

    public bool IsBattleWalkable(BattleHexCoord hex)
    {
        if (!IsInside(hex))
        {
            return false;
        }

        if (!boardNodeByBattleHex.TryGetValue(hex, out BoardNode node))
        {
            return false;
        }

        if (!boardState.TryGetTile(node, out BoardTileState tile))
        {
            return false;
        }

        return tile.Location == BoardType.Board;
    }

    public IEnumerable<BattleHexCoord> GetRing(BattleHexCoord center, int radius)
    {
        if (radius == 0)
        {
            if (IsInside(center))
            {
                yield return center;
            }

            yield break;
        }

        BattleHexCoord axialCenter = OffsetToAxial(center);

        BattleHexCoord axial = axialCenter + BattleHexCoord.Directions[4] * radius;

        for (int side = 0; side < 6; side++)
        {
            for (int step = 0; step < radius; step++)
            {
                BattleHexCoord offset = AxialToOffset(axial);

                if (IsInside(offset))
                {
                    yield return offset;
                }

                axial += BattleHexCoord.Directions[side];
            }
        }
    }
}