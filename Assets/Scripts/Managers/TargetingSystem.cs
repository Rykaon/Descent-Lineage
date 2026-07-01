using System.Collections.Generic;
using UnityEngine;

public class TargetingSystem
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
            unit.ClearDecision();

            if (unit.IsDead)
            {
                continue;
            }

            BattleUnitInstance currentTarget = BattleSystem.BattleState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

            if (CanKeepCurrentTarget(unit, currentTarget))
            {
                unit.DecisionReason = "Keeping current attack target";
                continue;
            }

            BattleUnitInstance newTarget = FindBestTargetByPath(unit, BattleSystem.BattleState);

            if (newTarget == null)
            {
                unit.RejectedAttackSlots.Clear();
                unit.RejectedTargets.Clear();
                unit.CurrentTargetBattleInstanceId = null;
                unit.ResetNavigationState();

                unit.Decision = BattleUnitDecision.NoTarget;
                unit.DecisionReason = "No enemy target found";
                continue;
            }

            if (unit.CurrentTargetBattleInstanceId != newTarget.BattleInstanceId)
            {
                unit.ClearSlotFailureMemory();
                unit.ResetNavigationState();
            }

            unit.CurrentTargetBattleInstanceId = newTarget.BattleInstanceId;
        }
    }

    public BattleUnitInstance FindClosestEnemy(BattleUnitInstance seeker, BattleState battleState)
    {
        BattleUnitInstance bestNonRejected = null;
        int bestNonRejectedDistance = int.MaxValue;

        BattleUnitInstance bestAny = null;
        int bestAnyDistance = int.MaxValue;

        foreach (var candidate in battleState.Units)
        {
            if (candidate == seeker)
            {
                continue;
            }

            if (candidate.IsDead)
            {
                continue;
            }

            if (candidate.OwnerPlayerId == seeker.OwnerPlayerId)
            {
                continue;
            }

            int distance = BattleRangeUtility.DistanceBetweenFootprints(seeker, candidate, BattleSystem);

            if (distance < bestAnyDistance)
            {
                bestAnyDistance = distance;
                bestAny = candidate;
            }

            if (seeker.RejectedTargets.ContainsKey(candidate.BattleInstanceId))
            {
                continue;
            }

            if (distance < bestNonRejectedDistance)
            {
                bestNonRejectedDistance = distance;
                bestNonRejected = candidate;
            }
        }

        if (bestNonRejected != null)
        {
            return bestNonRejected;
        }

        if (bestAny != null)
        {
            seeker.RejectedTargets.Clear();
            return bestAny;
        }

        return null;
    }

    private BattleUnitInstance FindBestTargetByPath(BattleUnitInstance seeker, BattleState battleState)
    {
        BattleUnitInstance bestNonRejected = null;
        int bestNonRejectedScore = int.MaxValue;

        BattleUnitInstance bestAny = null;
        int bestAnyScore = int.MaxValue;

        foreach (var candidate in battleState.Units)
        {
            if (candidate == seeker)
            {
                continue;
            }

            if (candidate.IsDead)
            {
                continue;
            }

            TargetQueryContext query = new(seeker, candidate);

            if (candidate.IsCamouflaged())
            {
                query.CanTarget = false;
            }

            BattleSystem.EffectSystem.TargetQuery(query);

            if (!query.CanTarget)
            {
                continue;
            }

            if (candidate.OwnerPlayerId == seeker.OwnerPlayerId)
            {
                continue;
            }

            int score = ScoreTargetByPath(seeker, candidate);

            if (query.PriorityBonus != 0)
            {
                score = query.PriorityBonus;
            }

            if (score < bestAnyScore)
            {
                bestAnyScore = score;
                bestAny = candidate;
            }

            if (seeker.RejectedTargets.ContainsKey(candidate.BattleInstanceId))
            {
                continue;
            }

            if (score < bestNonRejectedScore)
            {
                bestNonRejectedScore = score;
                bestNonRejected = candidate;
            }
        }

        if (bestNonRejected != null)
        {
            return bestNonRejected;
        }

        if (bestAny != null)
        {
            seeker.RejectedTargets.Clear();
            return bestAny;
        }

        return null;
    }

    private int ScoreTargetByPath(BattleUnitInstance seeker, BattleUnitInstance target)
    {
        if (BattleRangeUtility.CanAttack(seeker, target, BattleSystem))
        {
            return 0;
        }

        if (!TryGetBestAttackPathLength(seeker, target, out int pathLength))
        {
            return int.MaxValue / 2;
        }

        return pathLength;
    }

    private bool TryGetBestAttackPathLength(BattleUnitInstance seeker, BattleUnitInstance target, out int bestPathLength)
    {
        bestPathLength = int.MaxValue;

        int attackRange = BattleRangeUtility.GetAttackRange(seeker.AttackRangeTier);

        int minCenterDistance = 3;
        int maxCenterDistance = attackRange + 2;

        List<BattleHexCoord> pathBuffer = new();

        for (int d = minCenterDistance; d <= maxCenterDistance; d++)
        {
            foreach (BattleHexCoord center in BattleSystem.HexGrid.GetRing(target.CurrentHex, d))
            {
                if (!BattleSystem.HexGrid.CanOccupy(center, seeker, BattleSystem.HexOccupation))
                {
                    continue;
                }

                if (!BattleRangeUtility.CanAttackFromHex(seeker, center, target, BattleSystem.HexGrid))
                {
                    continue;
                }

                pathBuffer.Clear();

                bool pathFound = BattleSystem.HexPathfinder.TryFindPath(seeker.CurrentHex, center, BattleSystem.HexGrid, BattleSystem.HexOccupation, seeker, pathBuffer);

                if (!pathFound)
                {
                    continue;
                }

                if (pathBuffer.Count < bestPathLength)
                {
                    bestPathLength = pathBuffer.Count;
                }
            }
        }

        return bestPathLength < int.MaxValue;
    }

    private bool CanKeepCurrentTarget(BattleUnitInstance unit, BattleUnitInstance target)
    {
        if (target == null)
        {
            return false;
        }

        if (target.IsDead)
        {
            return false;
        }

        if (target.OwnerPlayerId == unit.OwnerPlayerId)
        {
            return false;
        }

        if (unit.RejectedTargets.ContainsKey(target.BattleInstanceId))
        {
            return false;
        }

        if (target.IsCamouflaged())
        {
            return false;
        }

        return false;
    }

    public void Clear()
    {

    }
}

public sealed class TargetQueryContext
{
    public BattleUnitInstance Seeker;
    public BattleUnitInstance Candidate;

    public bool CanTarget = true;
    public int PriorityBonus = 0;

    public TargetQueryContext(
        BattleUnitInstance seeker,
        BattleUnitInstance candidate)
    {
        Seeker = seeker;
        Candidate = candidate;
    }
}