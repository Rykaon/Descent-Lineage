using UnityEngine;

public abstract class BattleCladeEffectRuntime
{
    protected BattleSystem battleSystem;

    public virtual void Initialize(BattleSystem battleSystem)
    {
        this.battleSystem = battleSystem;
    }

    public virtual void ModifyStats(BattleUnitInstance unit, ActiveCladeEffect effect) { }

    public virtual void Tick(BattleUnitInstance unit, float deltaTime, ActiveCladeEffect effect) { }

    public virtual void OnUnitDeath(BattleUnitInstance deadUnit, ActiveCladeEffect effect) { }

    public virtual void OnAbilityCast(BattleUnitInstance caster, ActiveCladeEffect effect) { }

    protected bool TickInterval(BattleUnitInstance unit, string timerId, float deltaTime, float interval)
    {
        if (!unit.CladeEffectTimers.ContainsKey(timerId))
        {
            unit.CladeEffectTimers[timerId] = 0f;
        }

        unit.CladeEffectTimers[timerId] += deltaTime;

        if (unit.CladeEffectTimers[timerId] < interval)
        {
            return false;
        }

        unit.CladeEffectTimers[timerId] -= interval;
        return true;
    }

    protected float GetScaledValue(BattleUnitInstance unit, ActiveCladeEffect effect)
    {
        if (string.IsNullOrEmpty(effect.ScalingId))
        {
            return effect.Value;
        }

        if (!unit.CladeStacks.TryGetValue(effect.ScalingId, out int stacks))
        {
            return 0f;
        }

        return effect.Value * stacks;
    }
}

public sealed class NoCladeEffectRuntime : BattleCladeEffectRuntime
{
}

public sealed class AttackSpeedFlatCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void ModifyStats(BattleUnitInstance unit, ActiveCladeEffect effect)
    {
        unit.CurrentStats.AttackSpeed += GetScaledValue(unit, effect);
    }
}

public sealed class SlashOffenseFlatCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void ModifyStats(BattleUnitInstance unit, ActiveCladeEffect effect)
    {
        unit.CurrentStats.SlashOffense += Mathf.RoundToInt(GetScaledValue(unit, effect));
    }
}

public sealed class SlashDefenseFlatCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void ModifyStats(BattleUnitInstance unit, ActiveCladeEffect effect)
    {
        unit.CurrentStats.SlashDefense += Mathf.RoundToInt(GetScaledValue(unit, effect));
    }
}

public sealed class ImpactOffenseFlatCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void ModifyStats(BattleUnitInstance unit, ActiveCladeEffect effect)
    {
        unit.CurrentStats.ImpactOffense += Mathf.RoundToInt(GetScaledValue(unit, effect));
    }
}

public sealed class ImpactDefenseFlatCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void ModifyStats(BattleUnitInstance unit, ActiveCladeEffect effect)
    {
        unit.CurrentStats.ImpactDefense += Mathf.RoundToInt(GetScaledValue(unit, effect));
    }
}

public sealed class HealthRegenCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void Tick(BattleUnitInstance unit, float deltaTime, ActiveCladeEffect effect)
    {
        if (unit.IsDead || effect.Interval <= 0f)
        {
            return;
        }

        if (unit.CurrentHealth >= unit.MaxHealth)
        {
            return;
        }

        string timerId = $"Clade.{effect.CladeId}.{effect.EffectId}";

        if (!TickInterval(unit, timerId, deltaTime, effect.Interval))
        {
            return;
        }

        int heal = Mathf.RoundToInt(effect.Value);

        if (heal <= 0)
        {
            return;
        }

        int before = unit.CurrentHealth;

        unit.CurrentHealth = Mathf.Min(unit.CurrentHealth + heal, unit.MaxHealth);

        int healed = unit.CurrentHealth - before;

        if (healed > 0)
        {
            battleSystem.EventBuffer.AddHealEvent(unit, healed);
        }

        unit.CurrentHealth = Mathf.Min(unit.CurrentHealth + heal, unit.MaxHealth);
    }
}

public sealed class SharedGriefCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void OnUnitDeath(BattleUnitInstance deadUnit, ActiveCladeEffect effect)
    {
        if (!UnitHasClade(deadUnit, effect.CladeId))
        {
            return;
        }

        foreach (BattleUnitInstance ally in battleSystem.BattleState.Units)
        {
            if (ally.IsDead)
            {
                continue;
            }

            if (ally.OwnerPlayerId != deadUnit.OwnerPlayerId)
            {
                continue;
            }

            if (!UnitHasClade(ally, effect.CladeId))
            {
                continue;
            }

            AddStack(ally, "SharedGrief", 1);
        }
    }

    private bool UnitHasClade(BattleUnitInstance unit, string cladeId)
    {
        var definition = battleSystem.unitDatabase.GetUnit(unit.DefinitionId);
        return definition.Clades != null && System.Array.IndexOf(definition.Clades, cladeId) >= 0;
    }

    private void AddStack(BattleUnitInstance unit, string stackId, int amount)
    {
        if (!unit.CladeStacks.ContainsKey(stackId))
        {
            unit.CladeStacks[stackId] = 0;
        }

        unit.CladeStacks[stackId] += amount;
    }
}

public sealed class ManaLinkCladeEffectRuntime : BattleCladeEffectRuntime
{
    public override void OnAbilityCast(BattleUnitInstance caster, ActiveCladeEffect effect)
    {
        if (!UnitHasClade(caster, effect.CladeId))
        {
            return;
        }

        foreach (BattleUnitInstance ally in battleSystem.BattleState.Units)
        {
            if (ally.IsDead || ally == caster)
            {
                continue;
            }

            if (ally.OwnerPlayerId != caster.OwnerPlayerId)
            {
                continue;
            }

            if (!UnitHasClade(ally, effect.CladeId))
            {
                continue;
            }

            int manaGain = Mathf.RoundToInt(ally.CurrentStats.ManaMax * effect.Value);
            
            battleSystem.EventBuffer.AddManaEvent(ally, manaGain);
            
            ally.CurrentMana = Mathf.Min(ally.CurrentMana + manaGain, ally.CurrentStats.ManaMax);
        }
    }

    private bool UnitHasClade(BattleUnitInstance unit, string cladeId)
    {
        var definition = battleSystem.unitDatabase.GetUnit(unit.DefinitionId);
        return definition.Clades != null && System.Array.IndexOf(definition.Clades, cladeId) >= 0;
    }
}