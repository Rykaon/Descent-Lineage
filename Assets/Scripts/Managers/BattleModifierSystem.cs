using System.Collections.Generic;
using UnityEngine;

public sealed class BattleModifierSystem
{
    private BattleSystem BattleSystem;

    private readonly List<BattleStatModifier> modifierBuffer = new();

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
    }

    public void Tick(float deltaTime)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            RecalculateCurrentStats(unit);
        }
    }

    private void RecalculateCurrentStats(BattleUnitInstance unit)
    {
        unit.CurrentStats = CloneStats(unit.BaseStats);

        ApplyCladeModifiers(unit);

        modifierBuffer.Clear();

        foreach (BattleStatusRuntime status in unit.Statuses)
        {
            status.BuildModifiers(modifierBuffer);
        }

        foreach (BattleStatModifier modifier in modifierBuffer)
        {
            ApplyModifier(unit.CurrentStats, modifier);
        }

        ClampStats(unit);
    }

    private BaseStats CloneStats(BaseStats source)
    {
        return new BaseStats
        {
            HealthPoints = source.HealthPoints,
            AttackSpeed = source.AttackSpeed,
            ManaMax = source.ManaMax,
            ManaRegenPerAuto = source.ManaRegenPerAuto,
            ManaRegenPerDamage = source.ManaRegenPerDamage,
            SlashOffense = source.SlashOffense,
            ImpactOffense = source.ImpactOffense,
            SlashDefense = source.SlashDefense,
            ImpactDefense = source.ImpactDefense,
            MoveSpeed = source.MoveSpeed,
            AttackRange = source.AttackRange,
            CollisionShape = source.CollisionShape,
            CollisionRadius = source.CollisionRadius,
            CollisionHalfLength = source.CollisionHalfLength
        };
    }

    private void ApplyCladeModifiers(BattleUnitInstance unit)
    {
        foreach (ActiveCladeEffect effect in BattleSystem.CladeSystem.GetActiveEffects(unit.OwnerPlayerId))
        {
            BattleCladeEffectRuntime runtime = BattleCladeEffectFactory.Create(effect.EffectId);
            runtime.Initialize(BattleSystem);
            runtime.ModifyStats(unit, effect);
        }
    }

    private void ApplyModifier(BaseStats stats, BattleStatModifier modifier)
    {
        switch (modifier.StatType)
        {
            case BattleStatType.AttackSpeed:
                stats.AttackSpeed = Apply(stats.AttackSpeed, modifier);
                break;

            case BattleStatType.MoveSpeed:
                stats.MoveSpeed = Apply(stats.MoveSpeed, modifier);
                break;

            case BattleStatType.SlashOffense:
                stats.SlashOffense = Mathf.RoundToInt(Apply(stats.SlashOffense, modifier));
                break;

            case BattleStatType.ImpactOffense:
                stats.ImpactOffense = Mathf.RoundToInt(Apply(stats.ImpactOffense, modifier));
                break;

            case BattleStatType.SlashDefense:
                stats.SlashDefense = Mathf.RoundToInt(Apply(stats.SlashDefense, modifier));
                break;

            case BattleStatType.ImpactDefense:
                stats.ImpactDefense = Mathf.RoundToInt(Apply(stats.ImpactDefense, modifier));
                break;
        }
    }

    private float Apply(float currentValue, BattleStatModifier modifier)
    {
        return modifier.ModifierType switch
        {
            BattleStatModifierType.Add => currentValue + modifier.Value,
            BattleStatModifierType.Multiply => currentValue * modifier.Value,
            BattleStatModifierType.Override => modifier.Value,
            _ => currentValue
        };
    }

    private void ClampStats(BattleUnitInstance unit)
    {
        unit.CurrentStats.AttackSpeed = Mathf.Max(0.01f, unit.CurrentStats.AttackSpeed);
        unit.CurrentStats.MoveSpeed = Mathf.Max(0f, unit.CurrentStats.MoveSpeed);
    }

    public void Clear()
    {

    }
}