using System.Collections.Generic;
using UnityEngine;

public sealed class BattleHexOccupation
{
    private readonly BattleHexGrid grid;
    private readonly Dictionary<BattleHexCoord, BattleUnitInstance> occupants = new();
    private readonly Dictionary<BattleHexCoord, BattleUnitInstance> centerOccupants = new();

    public BattleHexOccupation(BattleHexGrid grid)
    {
        this.grid = grid;
    }

    public void Rebuild(BattleState state)
    {
        occupants.Clear();
        centerOccupants.Clear();

        foreach (var unit in state.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            centerOccupants[unit.CurrentHex] = unit;

            foreach (var hex in grid.GetUnitFootprint(unit.CurrentHex))
            {
                if (!grid.IsInside(hex))
                {
                    continue;
                }

                if (occupants.ContainsKey(hex))
                {
                    Debug.LogWarning($"Hex occupation conflict {hex}");
                }

                occupants[hex] = unit;
            }
        }
    }

    public BattleUnitInstance GetOccupant(BattleHexCoord hex)
    {
        occupants.TryGetValue(hex, out var unit);
        return unit;
    }

    public BattleUnitInstance GetCenterOccupant(BattleHexCoord hex)
    {
        centerOccupants.TryGetValue(hex, out var unit);
        return unit;
    }

    public bool IsOccupied(BattleHexCoord hex)
    {
        return occupants.ContainsKey(hex);
    }
}