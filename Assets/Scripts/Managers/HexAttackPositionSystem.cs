using System.Collections.Generic;
using UnityEngine;

public class HexAttackPositionSystem
{
    private BattleSystem BattleSystem;

    private readonly List<BattleHexCoord> pathBuffer = new();
    private readonly List<BattleHexCoord> bestPathBuffer = new();

    private const int MaxPathfindAttemptsPerUnit = 8;

    private readonly Dictionary<BattleHexCoord, BattleUnitInstance> reservedAttackCenters = new();

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public void Tick(float deltaTime)
    {
        reservedAttackCenters.Clear();

        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            if (unit.HasReservedAttackHex)
            {
                reservedAttackCenters[unit.ReservedAttackHex] = unit;
            }
        }

        foreach (var unit in BattleSystem.BattleState.Units)
        {
            TickUnit(unit, deltaTime);
        }
    }

    private void TickUnit(BattleUnitInstance unit, float deltaTime)
    {
        if (unit.IsDead)
        {
            return;
        }

        if (unit.IsEngaged)
        {
            return;
        }

        var target = BattleSystem.BattleState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
        {
            unit.ClearHexPath();
            return;
        }

        if (BattleRangeUtility.CanAttack(unit, target, BattleSystem))
        {
            return;
        }

        if (unit.HasHexPath)
        {
            var reservedTarget = BattleSystem.BattleState.GetUnitByBattleId(unit.ReservedAttackTargetId);

            if (reservedTarget == target && unit.HasReservedAttackHex && BattleRangeUtility.CanAttackFromHex(unit, unit.ReservedAttackHex, target, BattleSystem.HexGrid))
            {
                return;
            }

            unit.ClearHexPath();
            unit.HasReservedAttackHex = false;
            unit.ReservedAttackTargetId = null;
        }

        unit.HexRepathCooldown -= deltaTime;

        if (unit.HexRepathCooldown > 0f)
        {
            unit.Decision = BattleUnitDecision.WaitingForPath;
            unit.DecisionReason = "Hex repath cooldown";
            return;
        }

        TryBuildPathToAttackPosition(unit, target);
    }

    private bool TryBuildPathToAttackPosition(BattleUnitInstance attacker, BattleUnitInstance target)
    {
        List<BattleHexCoord> candidates = GetAttackCenters(attacker, target);

        candidates.Sort((a, b) =>
        {
            int scoreA = ScoreAttackCenter(attacker, target, a);
            int scoreB = ScoreAttackCenter(attacker, target, b);

            return scoreA.CompareTo(scoreB);
        });

        int attempts = 0;
        bool found = false;
        BattleHexCoord bestHex = attacker.CurrentHex;
        int bestScore = int.MaxValue;

        bestPathBuffer.Clear();

        foreach (var candidate in candidates)
        {
            if (attempts >= MaxPathfindAttemptsPerUnit)
            {
                break;
            }

            attempts++;

            pathBuffer.Clear();

            bool pathFound = BattleSystem.HexPathfinder.TryFindPath(attacker.CurrentHex, candidate, BattleSystem.HexGrid, BattleSystem.HexOccupation, attacker, pathBuffer);

            if (!pathFound || pathBuffer.Count == 0)
            {
                continue;
            }

            int score = pathBuffer.Count + ScoreAttackCenter(attacker, target, candidate);

            if (score < bestScore)
            {
                found = true;
                bestScore = score;
                bestHex = candidate;

                bestPathBuffer.Clear();
                bestPathBuffer.AddRange(pathBuffer);
            }
        }

        if (!found)
        {
            attacker.HasReservedAttackHex = false;
            attacker.ReservedAttackHex = default;
            attacker.ReservedAttackTargetId = null;

            attacker.HexRepathCooldown = Random.Range(0.15f, 0.25f);

            attacker.Decision = BattleUnitDecision.NoReachableAttackPosition;
            attacker.DecisionReason = "No reachable hex attack center";
            return false;
        }

        attacker.DesiredHex = bestHex;
        attacker.HexPath.Clear();
        attacker.HexPath.AddRange(bestPathBuffer);
        attacker.HexPathIndex = 0;

        attacker.HasReservedAttackHex = true;
        attacker.ReservedAttackHex = bestHex;
        attacker.ReservedAttackTargetId = target.BattleInstanceId;
        reservedAttackCenters[bestHex] = attacker;

        attacker.HexRepathCooldown = Random.Range(0.15f, 0.25f);

        attacker.Decision = BattleUnitDecision.MoveToReservedSlot;
        attacker.DecisionReason = "Hex attack center selected";

        /*Debug.Log(
            $"[RANGE SELECT] unit={attacker.BattleInstanceId} " +
            $"range={attacker.AttackRangeTier} " +
            $"selected={bestHex} " +
            $"footprintDistance={BattleRangeUtility.DistanceBetweenFootprintsFromHex(attacker, bestHex, target, BattleSystem.HexGrid)} " +
            $"pathCount={bestPathBuffer.Count}");*/

        return true;
    }

    private List<BattleHexCoord> GetAttackCenters(BattleUnitInstance attacker, BattleUnitInstance target)
    {
        List<BattleHexCoord> result = new();

        int attackRange = BattleRangeUtility.GetAttackRange(attacker.AttackRangeTier);

        int minCenterDistance = 3;
        int maxCenterDistance = attackRange + 2;

        bool preferMaxRange = attacker.AttackRangeTier != AttackRangeTier.Melee;

        if (preferMaxRange)
        {
            for (int d = maxCenterDistance; d >= minCenterDistance; d--)
            {
                AddAttackCentersAtDistance(attacker, target, d, result);

                if (result.Count > 0)
                {
                    break;
                }
            }
        }
        else
        {
            for (int d = minCenterDistance; d <= maxCenterDistance; d++)
            {
                AddAttackCentersAtDistance(attacker, target, d, result);

                if (result.Count > 0)
                {
                    break;
                }
            }
        }

        return result;
    }

    private void AddAttackCentersAtDistance(BattleUnitInstance attacker, BattleUnitInstance target, int centerDistance, List<BattleHexCoord> result)
    {
        foreach (BattleHexCoord center in BattleSystem.HexGrid.GetRing(target.CurrentHex, centerDistance))
        {
            if (!BattleSystem.HexGrid.CanOccupy(center, attacker, BattleSystem.HexOccupation))
            {
                continue;
            }

            if (IsReservedByOther(center, attacker))
            {
                continue;
            }

            if (!BattleRangeUtility.CanAttackFromHex(attacker, center, target, BattleSystem.HexGrid))
            {
                continue;
            }

            result.Add(center);
        }
    }

    private int ScoreAttackCenter(BattleUnitInstance attacker, BattleUnitInstance target, BattleHexCoord center)
    {
        int score = 0;

        int pathDistance = BattleSystem.HexGrid.Distance(attacker.CurrentHex, center);

        int footprintDistance = BattleRangeUtility.DistanceBetweenFootprintsFromHex(attacker, center, target, BattleSystem.HexGrid);

        int range = BattleRangeUtility.GetAttackRange(attacker.AttackRangeTier);

        bool isMelee = attacker.AttackRangeTier == AttackRangeTier.Melee;

        int idealDistance = isMelee ? 1 : range;

        score += pathDistance * 10;

        score += Mathf.Abs(footprintDistance - idealDistance) * 100;

        if (!isMelee && footprintDistance < idealDistance)
        {
            score += 500;
        }

        if (attacker.HasReservedAttackHex && attacker.ReservedAttackHex.Equals(center))
        {
            score -= 50;
        }

        return score;
    }

    private bool IsReservedByOther(BattleHexCoord center, BattleUnitInstance requester)
    {
        return reservedAttackCenters.TryGetValue(center, out BattleUnitInstance owner) && owner != requester && !owner.IsDead;
    }

    public void Clear()
    {

    }
}
