using System.Collections.Generic;
using UnityEngine;
using static Unity.Collections.AllocatorManager;

public class MovementSystem
{
    private BattleSystem BattleSystem;

    private readonly List<Vector2> pathBuffer = new();

    private const float ArrivalDistance = 0.06f;
    private const float RepathTargetDistance = 0.35f;
    private const float RepathInterval = 0.15f;
    public string debugUnitId;

    public void Initialize(BattleSystem system)
    {
        BattleSystem = system;
    }

    public void ComputeDesiredVelocities(float deltaTime)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
                continue;

            unit.DesiredVelocity = Vector2.zero;
            unit.FinalVelocity = Vector2.zero;

            unit.PhysicalBlockCooldown -= deltaTime;

            if (unit.PhysicalBlockCooldown < 0f)
                unit.PhysicalBlockCooldown = 0f;

            TickUnit(unit, deltaTime);
        }
    }

    public void ApplyMovement(float deltaTime)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead || unit.IsEngaged)
                continue;

            Vector2 velocity = unit.FinalVelocity;

            if (velocity.sqrMagnitude <= 0.001f)
            {
                continue;
            }

            unit.Position += unit.FinalVelocity * deltaTime;
            continue;

            Vector2 nextPosition = unit.Position + unit.FinalVelocity * deltaTime;

            if (CanMoveTo(unit, nextPosition, out BattleUnitInstance blocker))
            {
                unit.Position = nextPosition;
                //DebugMovementStall(unit, blocker, deltaTime);
                continue;
            }

            if (blocker != null)
            {
                Debug.DrawLine(
                    new Vector3(unit.Position.x, 1.4f, unit.Position.y),
                    new Vector3(blocker.Position.x, 1.4f, blocker.Position.y),
                    Color.black,
                    0.2f);
            }

            if (blocker != null && blocker.IsEngaged)
            {
                if (unit.PhysicalBlockCooldown <= 0f)
                {
                    /*if (unit.HasDesiredAttackPosition)
                    {
                        unit.RejectAttackSlot(
                            unit.DesiredAttackPosition,
                            BattleSystem.AttackSlotSystem.RejectedSlotDuration * 2f);

                        unit.RejectAttackSlot(
                            blocker.Position,
                            BattleSystem.AttackSlotSystem.RejectedSlotDuration * 2f);
                    }*/

                    unit.ResetNavigationState();

                    unit.RepathTimer = 0f;
                    unit.PhysicalBlockCooldown = 0.20f;

                    unit.Decision = BattleUnitDecision.WaitingForPath;
                    unit.DecisionReason = "Blocked by engaged obstacle, retrying another slot";
                }

                unit.FinalVelocity = Vector2.zero;

                DebugMovementStall(unit, blocker, deltaTime);

                continue;
            }

            unit.FinalVelocity = Vector2.zero;
        }
    }

    public void TickUnit(BattleUnitInstance unit, float deltaTime)
    {
        var target = BattleSystem.BattleState.GetUnitByBattleId(
            unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
        {
            unit.IsEngaged = false;

            unit.HasDesiredAttackPosition = false;
            unit.DesiredAttackPosition = default;

            unit.ResetNavigationState();

            unit.Decision = BattleUnitDecision.WaitingForTarget;
            unit.DecisionReason = "No valid target, waiting for TargetingSystem";
            return;
        }

        /*if (BattleRangeUtility.CanAttack(unit, target))
        {
            Engage(unit, target);
            return;
        }*/

        unit.IsEngaged = false;

        if (unit.DesiredPath.Count == 0)
        {
            unit.ResetNavigationState();
            unit.HasDesiredAttackPosition = false;

            unit.Decision = BattleUnitDecision.WaitingForPath;
            unit.DecisionReason = "No desired path, waiting for repath";

            Debug.LogWarning(
                $"[NO DESIRED PATH] {unit.BattleInstanceId} " +
                $"target={unit.CurrentTargetBattleInstanceId} " +
                $"hasSlot={unit.HasReservedAttackSlot} " +
                $"hasDesiredPos={unit.HasDesiredAttackPosition} " +
                $"repathTimer={unit.RepathTimer} " +
                $"rejectedTargets={unit.RejectedTargets.Count} " +
                $"rejectedSlots={unit.RejectedAttackSlots.Count}"
            );

            return;
        }

        if (!unit.HasDesiredAttackPosition)
        {
            unit.HasDesiredAttackPosition = true;

            if (unit.HasReservedAttackSlot)
                unit.DesiredAttackPosition = unit.ReservedAttackSlot;
            else
                unit.DesiredAttackPosition = unit.DesiredPath[^1];
        }

        bool pathEmpty = unit.Path.Waypoints.Count == 0;
        bool pathFinished = unit.Path.Waypoints.Count > 0 && !unit.Path.HasPath;
        bool desiredChanged = PathDoesNotMatchDesiredPath(unit);

        if (pathEmpty || desiredChanged)
        {
            unit.Path.SetPath(unit.DesiredPath);
        }
        else if (pathFinished)
        {
            unit.ResetNavigationState();

            unit.Decision = BattleUnitDecision.WaitingForPath;
            unit.DecisionReason = "Path finished but not in attack range, forcing repath";

            return;
        }

        unit.NavigationAge += deltaTime;

        PrepareMovement(unit);
        DetectNoProgressTowardGoal(unit, deltaTime);
    }

    private bool PathDoesNotMatchDesiredPath(BattleUnitInstance unit)
    {
        if (unit.Path.Waypoints.Count != unit.DesiredPath.Count)
            return true;

        if (unit.Path.Waypoints.Count == 0)
            return false;

        Vector2 currentEnd =
            unit.Path.Waypoints[^1];

        Vector2 desiredEnd =
            unit.DesiredPath[^1];

        return Vector2.Distance(currentEnd, desiredEnd) > 0.05f;
    }

    private void PrepareMovement(BattleUnitInstance unit)
    {
        if (!unit.Path.HasPath)
        {
            LogPrepareZero(unit, "No path at prepare start");
            unit.DesiredVelocity = Vector2.zero;
            return;
        }

        int safety = 0;

        while (unit.Path.HasPath && safety < 4)
        {
            Vector2 waypoint = unit.Path.CurrentWaypoint;
            float distanceToWaypoint = Vector2.Distance(unit.Position, waypoint);

            if (distanceToWaypoint > ArrivalDistance)
                break;

            unit.Path.Advance();
            safety++;
        }

        if (!unit.Path.HasPath)
        {
            LogPrepareZero(unit, "Path consumed after waypoint advance");
            unit.DesiredVelocity = Vector2.zero;
            return;
        }

        Vector2 currentWaypoint = unit.Path.CurrentWaypoint;
        Vector2 toWaypoint = currentWaypoint - unit.Position;

        if (toWaypoint.sqrMagnitude <= 0.0001f)
        {
            LogPrepareZero(unit, "Waypoint too close after advance");
            unit.DesiredVelocity = Vector2.zero;
            return;
        }

        Vector2 direction = toWaypoint.normalized;

        unit.DesiredVelocity = direction * unit.CurrentStats.MoveSpeed;
        unit.Forward = direction;
    }

    private void LogPrepareZero(BattleUnitInstance unit, string reason)
    {
        if (unit.DebugStallTimer < 0.75f)
            return;

        Debug.LogWarning(
            $"[PREPARE ZERO] {unit.BattleInstanceId} " +
            $"reason={reason} " +
            $"path={unit.Path.CurrentIndex}/{unit.Path.Waypoints.Count} " +
            $"hasPath={unit.Path.HasPath} " +
            $"pos={unit.Position} " +
            $"waypoint={(unit.Path.HasPath ? unit.Path.CurrentWaypoint.ToString() : "none")} " +
            $"dist={(unit.Path.HasPath ? Vector2.Distance(unit.Position, unit.Path.CurrentWaypoint).ToString() : "none")} " +
            $"desiredPath={unit.DesiredPath.Count} " +
            $"hasDesiredPos={unit.HasDesiredAttackPosition}"
        );
    }

    private bool CanMoveTo(
    BattleUnitInstance unit,
    Vector2 nextPosition,
    out BattleUnitInstance blocker)
    {
        blocker = null;

        const float Skin = 0.04f;
        const float MinSeparationProgress = 0.001f;

        Vector2 move = nextPosition - unit.Position;
        float unitRadius = unit.CurrentStats.CollisionRadius - Skin;

        foreach (var other in BattleSystem.BattleState.Units)
        {
            if (other == unit || other.IsDead)
                continue;

            bool otherIsCurrentTarget =
                other.BattleInstanceId == unit.CurrentTargetBattleInstanceId;

            if (otherIsCurrentTarget && IsAlmostInAttackRange(unit, other))
                continue;

            float currentDistanceToBody =
                DistanceToUnitBody(unit.Position, other);

            float nextDistanceToBody =
                DistanceToUnitBody(nextPosition, other);

            if (nextDistanceToBody >= unitRadius)
                continue;

            if (currentDistanceToBody < unitRadius &&
                nextDistanceToBody > currentDistanceToBody + MinSeparationProgress)
            {
                continue;
            }

            if (currentDistanceToBody > 0.0001f && move.sqrMagnitude > 0.0001f)
            {
                Vector2 normal = GetBodyNormal(unit.Position, other);
                float movingIntoOther = Vector2.Dot(move.normalized, -normal);

                if (movingIntoOther < 0.35f && nextDistanceToBody >= currentDistanceToBody - Skin)
                {
                    continue;
                }
            }

            blocker = other;

            if (other.IsEngaged)
            {
                if (nextDistanceToBody >= currentDistanceToBody - Skin)
                    continue;
            }

            return false;
        }

        return true;
    }

    private bool IsAlmostInAttackRange(BattleUnitInstance attacker, BattleUnitInstance target)
    {
        float distance = Vector2.Distance(attacker.Position, target.Position);

        float almostAttackDistance = attacker.CurrentStats.AttackRange + target.CurrentStats.CollisionRadius + 0.15f;

        return distance <= almostAttackDistance;
    }

    private float DistanceToUnitBody(
    Vector2 point,
    BattleUnitInstance other)
    {
        if (other.CurrentStats.CollisionShape == CollisionShapeType.Circle)
        {
            return Vector2.Distance(point, other.Position)
                - other.CurrentStats.CollisionRadius;
        }

        Vector2 forward = other.Forward;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector2.up;

        forward.Normalize();

        float halfLength = Mathf.Max(0f, other.CurrentStats.CollisionHalfLength - other.CurrentStats.CollisionRadius);

        Vector2 a = other.Position - forward * halfLength;
        Vector2 b = other.Position + forward * halfLength;

        return DistancePointSegment(point, a, b)
            - other.CurrentStats.CollisionRadius;
    }

    private Vector2 GetBodyNormal(
        Vector2 point,
        BattleUnitInstance other)
    {
        if (other.CurrentStats.CollisionShape == CollisionShapeType.Circle)
        {
            Vector2 delta = point - other.Position;

            if (delta.sqrMagnitude < 0.0001f)
                return Vector2.right;

            return delta.normalized;
        }

        Vector2 forward = other.Forward;

        if (forward.sqrMagnitude < 0.001f)
            forward = Vector2.up;

        forward.Normalize();

        float halfLength = Mathf.Max(0f, other.CurrentStats.CollisionHalfLength - other.CurrentStats.CollisionRadius);

        Vector2 a = other.Position - forward * halfLength;
        Vector2 b = other.Position + forward * halfLength;

        Vector2 closest = ClosestPointOnSegment(point, a, b);
        Vector2 normal = point - closest;

        if (normal.sqrMagnitude < 0.0001f)
            return Vector2.right;

        return normal.normalized;
    }

    private float DistancePointSegment(
    Vector2 p,
    Vector2 a,
    Vector2 b)
    {
        return Vector2.Distance(p, ClosestPointOnSegment(p, a, b));
    }

    private Vector2 ClosestPointOnSegment(
        Vector2 p,
        Vector2 a,
        Vector2 b)
    {
        Vector2 ab = b - a;

        if (ab.sqrMagnitude < 0.0001f)
            return a;

        float t = Vector2.Dot(p - a, ab) / ab.sqrMagnitude;
        t = Mathf.Clamp01(t);

        return a + ab * t;
    }

    private void Engage(
    BattleUnitInstance unit,
    BattleUnitInstance target)
    {
        unit.OnEngaged(true);

        Vector2 toTarget = target.Position - unit.Position;

        if (toTarget.sqrMagnitude > 0.001f)
        {
            unit.Forward = toTarget.normalized;
        }
    }

    private void DetectNoProgressTowardGoal(
    BattleUnitInstance unit,
    float deltaTime)
    {
        if (unit.IsDead || unit.IsEngaged || !unit.HasDesiredAttackPosition)
        {
            unit.NoProgressTimer = 0f;
            unit.LastDistanceToWaypoint = 0f;
            return;
        }

        if (unit.NavigationAge < 0.35f)
            return;

        if (!unit.Path.HasPath)
        {
            unit.NoProgressTimer = 0f;
            unit.LastDistanceToWaypoint = 0f;
            return;
        }

        if (!unit.HasLastNoProgressPosition)
        {
            unit.LastNoProgressPosition = unit.Position;
            unit.HasLastNoProgressPosition = true;
            return;
        }

        float movedDistance =
            Vector2.Distance(unit.Position, unit.LastNoProgressPosition);

        float expectedMove =
            unit.CurrentStats.MoveSpeed * deltaTime;

        float movementTolerance =
            Mathf.Max(0.001f, expectedMove * 0.25f);

        bool wantsMove =
            unit.DesiredVelocity.sqrMagnitude > 0.001f;

        bool didNotMoveEnough =
            movedDistance < movementTolerance;

        if (wantsMove && didNotMoveEnough)
            unit.NoProgressTimer += deltaTime;
        else
            unit.NoProgressTimer = 0f;

        unit.LastNoProgressPosition = unit.Position;

        if (unit.NoProgressTimer < 0.15f)
            return;

        Debug.LogWarning(
            $"[MOVE STATE] {unit.BattleInstanceId} " +
            $"target={unit.CurrentTargetBattleInstanceId} " +
            $"reserved={unit.HasReservedAttackSlot} " +
            $"slot={unit.ReservedAttackSlot} " +
            $"desiredPath={unit.DesiredPath.Count} " +
            $"path={unit.Path.CurrentIndex}/{unit.Path.Waypoints.Count} " +
            $"desired={unit.DesiredVelocity} " +
            $"final={unit.FinalVelocity}"
        );

        //BattleSystem.PathGrid.DrawDebugCosts(unit);
        DrawUnitDebug(unit, Color.magenta);
        DrawDesiredAttackPosition(unit);
        DrawTargetLink(unit);

        RequestRepathAfterNoProgress(unit);

        unit.NoProgressTimer = 0;
    }

    private void RequestRepathAfterNoProgress(BattleUnitInstance unit)
    {
        if (unit.HasDesiredAttackPosition)
        {
            //unit.RejectAttackSlot(unit.DesiredAttackPosition, BattleSystem.AttackSlotSystem.RejectedSlotDuration);
        }

        unit.ResetNavigationState();

        unit.Decision = BattleUnitDecision.WaitingForPath;
        unit.DecisionReason = "No progress, retrying another slot";
    }

    private void DebugMovementStall(BattleUnitInstance unit, BattleUnitInstance blocker, float deltaTime)
    {
        if (unit.IsDead || unit.IsEngaged)
            return;

        if (unit.Decision == BattleUnitDecision.WaitingForPath)
            return;

        bool hasMoveIntent =
    unit.DesiredVelocity.sqrMagnitude > 0.01f
    || unit.HasDesiredAttackPosition
    || unit.DesiredPath.Count > 0;

        bool isNotActuallyMoving =
            unit.FinalVelocity.sqrMagnitude <= 0.001f;

        if (!hasMoveIntent || !isNotActuallyMoving)
        {
            unit.DebugStallTimer = 0f;
            return;
        }

        unit.DebugStallTimer += deltaTime;

        if (unit.DebugStallTimer < 0.75f)
            return;

        Debug.LogWarning(
            $"[STATE] {unit.BattleInstanceId} " +
            $"engaged={unit.IsEngaged} " +
            $"target={unit.CurrentTargetBattleInstanceId} " +
            $"desiredPath={unit.DesiredPath.Count} " +
            $"pathHasPath={unit.Path.HasPath} " +
            $"pathIndex={unit.Path.CurrentIndex}/{unit.Path.Waypoints.Count} " +
            $"desired={unit.DesiredVelocity} " +
            $"final={unit.FinalVelocity} " +
            $"reserved={unit.HasReservedAttackSlot} " +
            $"desiredPos={unit.HasDesiredAttackPosition} " +
            $"blocker={(blocker != null ? blocker.BattleInstanceId : "none")} " +
            $"blockerEngaged={(blocker != null ? blocker.IsEngaged.ToString() : "none")} "
        );

        DrawUnitDebug(unit, Color.magenta);
        DrawDesiredAttackPosition(unit);
        DrawTargetLink(unit);
    }

    private void DrawUnitDebug(
    BattleUnitInstance unit,
    Color color)
    {
        Vector3 pos = new Vector3(unit.Position.x, 0.8f, unit.Position.y);

        Debug.DrawLine(
            pos + Vector3.left * 0.2f,
            pos + Vector3.right * 0.2f,
            color,
            0.05f);

        Debug.DrawLine(
            pos + Vector3.forward * 0.2f,
            pos + Vector3.back * 0.2f,
            color,
            0.05f);
    }

    private void DrawDesiredAttackPosition(BattleUnitInstance unit)
    {
        if (!unit.HasDesiredAttackPosition)
            return;

        Vector3 p = new Vector3(
            unit.DesiredAttackPosition.x,
            0.7f,
            unit.DesiredAttackPosition.y);

        Debug.DrawLine(p + Vector3.left * 0.15f, p + Vector3.right * 0.15f, Color.cyan, 0.05f);
        Debug.DrawLine(p + Vector3.forward * 0.15f, p + Vector3.back * 0.15f, Color.cyan, 0.05f);

        Debug.DrawLine(
            new Vector3(unit.Position.x, 0.7f, unit.Position.y),
            p,
            Color.cyan,
            0.05f);
    }

    private void DrawTargetLink(BattleUnitInstance unit)
    {
        if (string.IsNullOrEmpty(unit.CurrentTargetBattleInstanceId))
        {
            Debug.LogWarning($"[DRAW TARGET LINK] {unit.BattleInstanceId} has no target id");
            return;
        }

        var target = BattleSystem.BattleState.GetUnitByBattleId(
            unit.CurrentTargetBattleInstanceId);

        if (target == null)
        {
            Debug.LogWarning(
                $"[DRAW TARGET LINK] {unit.BattleInstanceId} target not found: {unit.CurrentTargetBattleInstanceId}");
            return;
        }

        if (target.IsDead)
        {
            Debug.LogWarning(
                $"[DRAW TARGET LINK] {unit.BattleInstanceId} target dead: {unit.CurrentTargetBattleInstanceId}");
            return;
        }

        Vector3 from = new Vector3(unit.Position.x, 1.25f, unit.Position.y);
        Vector3 to = new Vector3(target.Position.x, 1.25f, target.Position.y);

        Debug.DrawLine(from, to, Color.blue, 0.15f);

        Debug.DrawLine(
            to + Vector3.left * 0.25f,
            to + Vector3.right * 0.25f,
            Color.blue,
            0.15f);

        Debug.DrawLine(
            to + Vector3.forward * 0.25f,
            to + Vector3.back * 0.25f,
            Color.blue,
            0.15f);
    }

    public void Clear()
    {
        pathBuffer.Clear();
        debugUnitId = null;
}
}

public class BattlePathState
{
    public readonly List<Vector2> Waypoints = new();
    public int CurrentIndex;

    public bool HasPath => CurrentIndex < Waypoints.Count;

    public Vector2 CurrentWaypoint => Waypoints[CurrentIndex];

    public void Clear()
    {
        Waypoints.Clear();
        CurrentIndex = 0;
    }

    public void SetPath(List<Vector2> path)
    {
        Waypoints.Clear();
        Waypoints.AddRange(path);
        CurrentIndex = 0;
    }

    public void Advance()
    {
        CurrentIndex++;
    }
}