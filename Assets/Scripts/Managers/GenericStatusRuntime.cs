using System.Collections.Generic;
using System;
using UnityEngine;

public sealed class GenericStatusRuntime : BattleStatusRuntime
{
    private readonly List<BattleStatModifier> modifiers = new();

    public int StackCount { get; private set; } = 1;
    public int MaxStacks { get; private set; } = 1;

    public void SetupStacks(int initialStacks, int maxStacks)
    {
        StackCount = initialStacks;
        MaxStacks = maxStacks;
    }

    public override bool TryMergeWith(BattleStatusRuntime other)
    {
        if (other is not GenericStatusRuntime otherGeneric)
        {
            return false;
        }

        StackCount = Math.Min(MaxStacks, StackCount + otherGeneric.StackCount);
        DurationRemaining = Math.Max(DurationRemaining, otherGeneric.DurationRemaining);

        return true;
    }

    public void AddModifier(BattleStatType statType, BattleStatModifierType modifierType, float value)
    {
        modifiers.Add(new BattleStatModifier(statType, modifierType, value));
    }

    public override void BuildModifiers(List<BattleStatModifier> buffer)
    {
        buffer.AddRange(modifiers);
    }

    public override bool CanStackWith(BattleStatusRuntime other)
    {
        return other.StatusId != StatusId;
    }

    public override void RefreshFrom(BattleStatusRuntime other)
    {
        DurationRemaining = other.DurationRemaining;

        if (other is not GenericStatusRuntime otherStatModifierStatus)
        {
            return;
        }

        modifiers.Clear();

        foreach (BattleStatModifier modifier in otherStatModifierStatus.modifiers)
        {
            modifiers.Add(new BattleStatModifier(modifier.StatType, modifier.ModifierType, modifier.Value));
        }
    }
}

public sealed class PoisonStatusRuntime : BattleStatusRuntime
{
    private int stackCount;
    private int maxStacks;

    private int damagePerStack;
    private float tickInterval;
    private float tickTimer;

    public int StackCount => stackCount;

    public void Setup(int initialStacks, int maxStacks, int damagePerStack, float tickInterval)
    {
        stackCount = Mathf.Clamp(initialStacks, 1, maxStacks);
        this.maxStacks = maxStacks;
        this.damagePerStack = damagePerStack;
        this.tickInterval = tickInterval;
        tickTimer = tickInterval;
    }

    public void ClampStacks(int maxAllowedStacks)
    {
        stackCount = Mathf.Min(stackCount, maxAllowedStacks);
        maxStacks = Mathf.Min(maxStacks, maxAllowedStacks);
    }

    public override void Tick(BattleSystem battleSystem, float deltaTime)
    {
        base.Tick(battleSystem, deltaTime);

        tickTimer -= deltaTime;

        if (tickTimer > 0f)
        {
            return;
        }

        tickTimer += tickInterval;

        int damage = damagePerStack * stackCount;

        if (damage <= 0)
        {
            Debug.Log($"[POISON TICK] target={TargetUnit.BattleInstanceId} damage={damage} stacks={StackCount}");
            return;
        }

        DamageContext context = new(SourceUnit, TargetUnit, null, damage, DamageDelivery.DamageOverTime);
        context.FinalDamage = damage;
        Debug.Log($"[POISON TICK] target={TargetUnit.BattleInstanceId} damage={damage} stacks={StackCount}");
        TargetUnit.PendingDamageContexts.Add(context);
    }

    public override bool TryMergeWith(BattleStatusRuntime other)
    {
        if (other is not PoisonStatusRuntime otherPoison)
        {
            return false;
        }

        stackCount = Mathf.Min(maxStacks, stackCount + otherPoison.stackCount);
        DurationRemaining = Mathf.Max(DurationRemaining, otherPoison.DurationRemaining);

        return true;
    }

    public override bool CanStackWith(BattleStatusRuntime other)
    {
        return false;
    }
}