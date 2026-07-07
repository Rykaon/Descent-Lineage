using System.Collections.Generic;
using UnityEngine;

public sealed class BattleClientUnit
{
    public string BattleInstanceId;
    public string BoardInstanceId;
    public string DefinitionId;

    public int OwnerPlayerId;

    public BattleHexCoord CurrentHex;

    public Vector2 Position;
    public Vector2 LastPosition;

    public int CurrentHealth;
    public int MaxHealth;
    public int CurrentMana;
    public int MaxMana;
    public float AttackSpeed;
    public float MoveSpeed;

    public bool IsDead;

    public string CurrentTargetBattleInstanceId;
}

public sealed class BattleClientState
{
    public readonly List<BattleClientUnit> Units = new();
    private readonly Dictionary<string, BattleClientUnit> unitByBattleId = new();

    public void Clear()
    {
        Units.Clear();
        unitByBattleId.Clear();
    }

    public void AddUnit(BattleClientUnit unit)
    {
        Units.Add(unit);
        unitByBattleId[unit.BattleInstanceId] = unit;
    }

    public BattleClientUnit GetUnitByBattleId(string battleInstanceId)
    {
        if (string.IsNullOrEmpty(battleInstanceId))
        {
            return null;
        }

        unitByBattleId.TryGetValue(battleInstanceId, out BattleClientUnit unit);
        return unit;
    }
}