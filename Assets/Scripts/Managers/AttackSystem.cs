using System.Collections.Generic;
using UnityEngine;

public class AttackSystem
{
    private BattleSystem BattleSystem;

    public void Initialize(BattleSystem system)
    {
        BattleSystem = system;
    }

    public void Tick(float deltaTime)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            TickUnit(unit, deltaTime);
        }
    }

    private void TickUnit(BattleUnitInstance unit, float deltaTime)
    {
        var target = BattleSystem.BattleState.GetUnitByBattleId(
            unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
        {
            unit.ClearAttackSlotReservation();
            unit.CurrentTargetBattleInstanceId = null;
            return;
        }

        if (!unit.IsEngaged)
        {
            return;
        }

        if (!BattleRangeUtility.CanAttack(unit, target, BattleSystem))
        {
            return;
        }

        Vector2 toTarget = BattleSystem.HexGrid.HexToWorld(target.CurrentHex) - BattleSystem.HexGrid.HexToWorld(unit.CurrentHex);

        if (toTarget.sqrMagnitude > 0.001f)
        {
            unit.Forward = toTarget.normalized;
        }

        unit.AttackCooldownRemaining -= deltaTime;

        if (unit.AttackCooldownRemaining > 0f)
        {
            return;
        }

        ApplyAttack(unit, target, unit.BasicAttackDamageProfile);

        unit.AttackCooldownRemaining = 1f / Mathf.Max(unit.CurrentStats.AttackSpeed, 0.01f);
    }

    public int ComputeDamage(BaseStats attacker, BaseStats target, DamageProfile profile)
    {
        float totalDamage = 0f;

        foreach (var weight in profile.Weights)
        {
            int offense = GetOffense(attacker, weight.Type);
            int defense = GetDefense(target, weight.Type);

            int typedDamage = Mathf.Max(1, offense - defense);

            totalDamage += typedDamage * weight.Weight;
        }

        return Mathf.Max(1, Mathf.RoundToInt(totalDamage));
    }

    private int GetOffense(BaseStats stats, DamageType type)
    {
        return type switch
        {
            DamageType.Slash => stats.SlashOffense,
            DamageType.Impact => stats.ImpactOffense,
            _ => 0
        };
    }

    private int GetDefense(BaseStats stats, DamageType type)
    {
        return type switch
        {
            DamageType.Slash => stats.SlashDefense,
            DamageType.Impact => stats.ImpactDefense,
            _ => 0
        };
    }

    private void ApplyAttack(BattleUnitInstance attacker, BattleUnitInstance target, DamageProfile profile)
    {
        int damage = ComputeDamage(attacker.CurrentStats, target.CurrentStats, profile);

        target.PendingDamage += damage;
    }

    public void Clear()
    {
        
    }
}

public static class BattleRangeUtility
{
    private const float AttackRangeTolerance = 0.05f;
    private const float EngagementExitTolerance = 0.20f;

    public static bool CanAttack(BattleUnitInstance attacker, BattleUnitInstance target, BattleSystem system)
    {
        int distance = DistanceBetweenFootprints(attacker, target, system);
        int range = GetAttackRange(attacker.AttackRangeTier);

        return distance <= range;
    }

    public static int DistanceBetweenFootprints(BattleUnitInstance a, BattleUnitInstance b, BattleSystem system)
    {
        List<BattleHexCoord> footprintA = system.HexGrid.GetUnitFootprint(a.CurrentHex);

        List<BattleHexCoord> footprintB = system.HexGrid.GetUnitFootprint(b.CurrentHex);

        int best = int.MaxValue;

        foreach (BattleHexCoord hexA in footprintA)
        {
            foreach (BattleHexCoord hexB in footprintB)
            {
                int distance = system.HexGrid.Distance(hexA, hexB);

                if (distance < best)
                {
                    best = distance;
                }
            }
        }

        return best;
    }

    public static int DistanceBetweenFootprintsFromHex(BattleUnitInstance attacker, BattleHexCoord attackerCenter, BattleUnitInstance target, BattleHexGrid grid)
    {
        var attackerFootprint = grid.GetUnitFootprint(attackerCenter);
        var targetFootprint = grid.GetUnitFootprint(target.CurrentHex);

        int best = int.MaxValue;

        foreach (var hexA in attackerFootprint)
        {
            foreach (var hexB in targetFootprint)
            {
                int distance = grid.Distance(hexA, hexB);

                if (distance < best)
                {
                    best = distance;
                }
            }
        }

        return best;
    }

    public static int GetAttackRange(AttackRangeTier tier)
    {
        return tier switch
        {
            AttackRangeTier.Melee => 1,
            AttackRangeTier.Short => 6,
            AttackRangeTier.Medium => 9,
            AttackRangeTier.Long => 12,
            AttackRangeTier.VeryLong => 15,
            _ => 1
        };
    }

    public static bool CanAttackFromHex(BattleUnitInstance attacker, BattleHexCoord attackerCenter, BattleUnitInstance target, BattleHexGrid grid)
    {
        var attackerFootprint = grid.GetUnitFootprint(attackerCenter);
        var targetFootprint = grid.GetUnitFootprint(target.CurrentHex);

        int best = int.MaxValue;

        foreach (var hexA in attackerFootprint)
        {
            foreach (var hexB in targetFootprint)
            {
                int distance = grid.Distance(hexA, hexB);

                if (distance < best)
                {
                    best = distance;
                }
            }
        }

        return best <= GetAttackRange(attacker.AttackRangeTier);
    }
}