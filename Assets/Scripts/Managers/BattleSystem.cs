using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class BattleSystem
{
    public SharedBoardState BoardState { get; private set; }
    public BattleState BattleState { get; private set; }
    public BattleStateBuilder BattleStateBuilder { get; private set; }

    public BattleHexGrid HexGrid { get; private set; }
    public BattleHexOccupation HexOccupation { get; private set; }
    public BattleHexPathfinder HexPathfinder { get; private set; }

    public TargetingSystem TargetingSystem { get; private set; }
    public HexAttackPositionSystem HexAttackPositionSystem { get; private set; }
    public HexMovementSystem HexMovementSystem { get; private set; }
    public HexEngagementSystem HexEngagementSystem { get; private set; }
    public AttackSystem AttackSystem { get; private set; }
    public DeathSystem DeathSystem { get; private set; }

    public IUnitDefinitionDatabase unitDatabase { get; private set; }
    public IMutationDefinitionDatabase mutationDatabase { get; private set; }

    public event Action<BattleUnitInstance, int> OnDamageApplied;

    public void Initialize(IUnitDefinitionDatabase unitDatabase, IMutationDefinitionDatabase mutationDatabase, SharedBoardState state)
    {
        BoardState = state;
        this.unitDatabase = unitDatabase;
        this.mutationDatabase = mutationDatabase;

        BattleStateBuilder = new BattleStateBuilder();

        HexGrid = new BattleHexGrid(BoardState);

        HexOccupation = new BattleHexOccupation(HexGrid);
        HexPathfinder = new BattleHexPathfinder();

        HexPathfinder.Initialize(this);

        TargetingSystem = new TargetingSystem();
        HexAttackPositionSystem = new HexAttackPositionSystem();
        HexMovementSystem = new HexMovementSystem();
        HexEngagementSystem = new HexEngagementSystem();
        AttackSystem = new AttackSystem();
        DeathSystem = new DeathSystem();


        TargetingSystem.Initialize(this);
        HexAttackPositionSystem.Initialize(this);
        HexMovementSystem.Initialize(this);
        HexEngagementSystem.Initialize(this);
        AttackSystem.Initialize(this);
        DeathSystem.Initialize(this);
    }

    public void StartBattle(GameState gameState)
    {
        ClearBattle();

        BattleState = BattleStateBuilder.Build(gameState, this);
    }

    public void Tick(float deltaTime)
    {
        HexOccupation.Rebuild(BattleState);

        TargetingSystem.Tick(deltaTime);
        HexAttackPositionSystem.Tick(deltaTime);
        HexMovementSystem.Tick(deltaTime);
        HexEngagementSystem.Tick(deltaTime);
        AttackSystem.Tick(deltaTime);
        DeathSystem.Tick();
    }

    public void RaiseDamageApplied(BattleUnitInstance target, int damage)
    {
        OnDamageApplied?.Invoke(target, damage);
    }

    public bool TryGetWinner(BattleState battleState, out int winnerPlayerId)
    {
        winnerPlayerId = -1;

        HashSet<int> livingPlayers = new();

        foreach (var unit in battleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            livingPlayers.Add(unit.OwnerPlayerId);
        }

        if (livingPlayers.Count != 1)
        {
            return false;
        }

        winnerPlayerId = livingPlayers.First();

        return true;
    }

    public void ClearBattle()
    {
        BattleState?.Clear();

        BattleState = null;

        TargetingSystem.Clear();
        HexAttackPositionSystem.Clear();
        HexMovementSystem.Clear();
        HexEngagementSystem.Clear();
        AttackSystem.Clear();
        DeathSystem.Clear();

        
    }
}

public sealed class BattleUnitInstance
{
    public string BattleInstanceId;
    public string BoardInstanceId;
    public string DefinitionId;

    public int OwnerPlayerId;

    public Vector2 LastPosition;
    public Vector2 Position;

    public List<string> MutationIds = new();

    public int MaxHealth;
    public int CurrentHealth;

    public BaseStats BaseStats;
    public BaseStats CurrentStats;

    public DamageProfile BasicAttackDamageProfile;
    public DamageProfile AbilityDamageProfile;

    public AttackRangeTier AttackRangeTier;
    public CollisionBodyPreset CollisionBodyPreset;

    public int PendingDamage;

    public float AttackCooldownRemaining;
    public float TimeWithoutMoving;

    public bool IsEngaged;
    public bool IsDead;

    public NavPresence NavPresence = NavPresence.AgentOnly;
    public float RepathTimer;

    public BattlePathState Path = new();

    public readonly List<Vector2> DesiredPath = new();
    public Vector2 LastPathTargetPosition;

    public string CurrentTargetBattleInstanceId;
    public Vector2 CurrentMoveDestination;
    public Vector2 Forward;

    public readonly Dictionary<string, float> RejectedTargets = new();

    public readonly Dictionary<Vector2, float> RejectedAttackSlots = new();
    public string LastTargetBattleInstanceId;

    public Vector2 DesiredVelocity;
    public Vector2 FinalVelocity;

    public float DebugBlockedTimer;
    public Vector2 DebugLastPosition;

    public BattleUnitDecision Decision;
    public string DecisionReason;

    public bool HasDesiredAttackPosition;
    public Vector2 DesiredAttackPosition;

    public float NoProgressTimer;
    public float LastDistanceToWaypoint;
    public bool WasEngagedLastFrame;
    public float TimeSinceNotEngaged;
    public float TimeSinceEngaged;

    public float DebugStallTimer;

    public float NavigationAge;
    public Vector2 LastNoProgressPosition;
    public bool HasLastNoProgressPosition;
    public float PhysicalBlockCooldown;

    public int AvoidanceSide;
    public float AvoidanceSideLockTimer;
    public string AvoidanceObstacleId;
    public float BypassTimer;
    public Vector2 BypassDirection;
    public string BypassObstacleId;








    public BattleHexCoord CurrentHex;
    public BattleHexCoord DesiredHex;

    public readonly List<BattleHexCoord> HexPath = new();
    public bool HasHexPath => HexPath != null && HexPathIndex >= 0 && HexPathIndex < HexPath.Count;
    public int HexPathIndex;

    public BattleHexCoord ReservedAttackHex;
    public bool HasReservedAttackHex;
    public string ReservedAttackTargetId;
    public float HexMoveBudget;
    public float HexRepathCooldown;

    public bool HasPreparedHexMove;
    public BattleHexCoord PreparedHexMove;

    public void ClearDecision()
    {
        Decision = BattleUnitDecision.None;
        DecisionReason = null;
    }

    public void ClearNavigationPath()
    {
        DesiredPath.Clear();
        Path.Clear();

        DesiredVelocity = Vector2.zero;
        FinalVelocity = Vector2.zero;

        RepathTimer = 0f;

        NoProgressTimer = 0f;
        LastDistanceToWaypoint = 0f;
        NavigationAge = 0f;
        LastNoProgressPosition = default;
        HasLastNoProgressPosition = false;
    }

    public void ClearSlotFailureMemory()
    {
        RejectedAttackSlots.Clear();
    }

    public void ClearTargetFailureMemory()
    {
        RejectedTargets.Clear();
    }

    public void ResetNavigationState()
    {
        ClearHexPath();

        HasReservedAttackHex = false;
        ReservedAttackHex = default;
    }

    public void RejectAttackSlot(Vector2 slot, float duration)
    {
        RejectedAttackSlots[slot] = duration;
    }

    public void RejectCurrentTarget(float duration)
    {
        if (!string.IsNullOrEmpty(CurrentTargetBattleInstanceId))
        {
            RejectedTargets[CurrentTargetBattleInstanceId] = duration;
        }

        CurrentTargetBattleInstanceId = null;

        ResetNavigationState();
    }

    public void OnEngaged(bool value)
    {
        IsEngaged = value;

        if (IsEngaged)
        {
            ResetNavigationState();

            ClearSlotFailureMemory();
            ClearTargetFailureMemory();

            Decision = BattleUnitDecision.Attack;
            DecisionReason = "Target in attack range";
        }
    }

    public void ClearHexPath()
    {
        HexPath.Clear();
        HexPathIndex = 0;
        DesiredHex = CurrentHex;
    }
}

public struct BattlePosition
{
    public Vector2 Value; 
    public BattlePosition(Vector2 value) 
    {
        Value = value;
    }
}

public static class BattleDebugDraw
{
    public static bool Enabled = true;

    public static bool DrawAttackState = true;
    public static bool DrawPaths = true;
    public static bool DrawVelocities = true;
    public static bool DrawObstacleShapes = true;
    public static bool DrawReservedSlots = true;

    public static void DrawUnitDebug(
        BattleUnitInstance unit,
        BattleState state)
    {
        if (!Enabled || unit.IsDead)
            return;

        if (DrawAttackState)
            DrawEngagement(unit, state);

        if (DrawPaths)
            DrawPath(unit);

        if (DrawVelocities)
            DrawVelocitiesDebug(unit);

        if (DrawObstacleShapes)
            DrawNavObstacleShape(unit);
    }

    private static void DrawEngagement(
        BattleUnitInstance unit,
        BattleState state)
    {
        var target = state.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
            return;

        Color color = unit.IsEngaged ? Color.red : Color.yellow;

        Debug.DrawLine(
            To3D(unit.Position, 0.15f),
            To3D(target.Position, 0.15f),
            color);
    }

    private static void DrawReservedSlot(BattleUnitInstance unit)
    {
        if (!unit.HasReservedAttackSlot)
            return;

        DrawCross(
            unit.ReservedAttackSlot,
            0.12f,
            Color.cyan);

        Debug.DrawLine(
            To3D(unit.Position, 0.1f),
            To3D(unit.ReservedAttackSlot, 0.1f),
            Color.cyan);
    }

    private static void DrawPath(BattleUnitInstance unit)
    {
        if (unit.Path == null || !unit.Path.HasPath)
            return;

        Vector2 previous = unit.Position;

        for (int i = unit.Path.CurrentIndex; i < unit.Path.Waypoints.Count; i++)
        {
            Vector2 current = unit.Path.Waypoints[i];

            Debug.DrawLine(
                To3D(previous, 0.05f),
                To3D(current, 0.05f),
                Color.green);

            DrawCross(current, 0.06f, Color.green);

            previous = current;
        }
    }

    private static void DrawVelocitiesDebug(BattleUnitInstance unit)
    {
        if (unit.DesiredVelocity.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(
                To3D(unit.Position, 0.2f),
                To3DVector(unit.DesiredVelocity.normalized * 0.4f),
                Color.blue);
        }

        if (unit.FinalVelocity.sqrMagnitude > 0.001f)
        {
            Debug.DrawRay(
                To3D(unit.Position, 0.25f),
                To3DVector(unit.FinalVelocity.normalized * 0.4f),
                Color.magenta);
        }
    }

    private static void DrawNavObstacleShape(BattleUnitInstance unit)
    {
        if (unit.NavPresence != NavPresence.HardObstacle)
            return;

        if (unit.CurrentStats.CollisionShape == CollisionShapeType.Circle)
        {
            DrawCircle(
                unit.Position,
                unit.CurrentStats.CollisionRadius,
                Color.red);
        }
        else
        {
            DrawCapsule(
                unit.Position,
                unit.Forward,
                unit.CurrentStats.CollisionRadius*0.5f,
                unit.CurrentStats.CollisionHalfLength*0.5f,
                Color.red);
        }
    }

    public static void DrawCircle(
        Vector2 center,
        float radius,
        Color color,
        int segments = 24)
    {
        float step = Mathf.PI * 2f / segments;

        Vector2 previous = center + new Vector2(radius, 0f);

        for (int i = 1; i <= segments; i++)
        {
            float angle = i * step;
            Vector2 current = center + new Vector2(
                Mathf.Cos(angle) * radius,
                Mathf.Sin(angle) * radius);

            Debug.DrawLine(
                To3D(previous, 0.3f),
                To3D(current, 0.3f),
                color);

            previous = current;
        }
    }

    public static void DrawCapsule(
        Vector2 center,
        Vector2 forward,
        float radius,
        float halfLength,
        Color color,
        int segments = 12)
    {
        if (forward.sqrMagnitude < 0.001f)
            forward = Vector2.up;

        forward.Normalize();

        Vector2 right = new Vector2(-forward.y, forward.x);

        Vector2 a = center - forward * halfLength;
        Vector2 b = center + forward * halfLength;

        Debug.DrawLine(To3D(a + right * radius, 0.3f), To3D(b + right * radius, 0.3f), color);
        Debug.DrawLine(To3D(a - right * radius, 0.3f), To3D(b - right * radius, 0.3f), color);

        DrawArc(a, -forward, radius, color, segments);
        DrawArc(b, forward, radius, color, segments);
    }

    private static void DrawArc(
        Vector2 center,
        Vector2 forward,
        float radius,
        Color color,
        int segments)
    {
        Vector2 right = new Vector2(-forward.y, forward.x);

        Vector2 previous = center + right * radius;

        for (int i = 1; i <= segments; i++)
        {
            float t = i / (float)segments;
            float angle = Mathf.Lerp(0f, Mathf.PI, t);

            Vector2 dir =
                Mathf.Cos(angle) * right
                + Mathf.Sin(angle) * forward;

            Vector2 current = center + dir * radius;

            Debug.DrawLine(
                To3D(previous, 0.3f),
                To3D(current, 0.3f),
                color);

            previous = current;
        }
    }

    private static void DrawCross(
        Vector2 position,
        float size,
        Color color)
    {
        Debug.DrawLine(
            To3D(position + Vector2.left * size, 0.35f),
            To3D(position + Vector2.right * size, 0.35f),
            color);

        Debug.DrawLine(
            To3D(position + Vector2.down * size, 0.35f),
            To3D(position + Vector2.up * size, 0.35f),
            color);
    }

    private static Vector3 To3D(Vector2 position, float y)
    {
        return new Vector3(position.x, y, position.y);
    }

    private static Vector3 To3DVector(Vector2 vector)
    {
        return new Vector3(vector.x, 0f, vector.y);
    }
}