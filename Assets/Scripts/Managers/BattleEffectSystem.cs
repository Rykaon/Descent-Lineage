using System;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public enum BattleEffectSource
{
    Mutation,
    InnateKeyword,
    CladeSynergy,
    StatusEffect
}

public enum DamageDelivery
{
    DirectAttack,
    DamageOverTime,
    Ability,
    TrueDamage
}

public sealed class DamageContext
{
    public BattleUnitInstance Attacker;
    public BattleUnitInstance Target;

    public DamageProfile Profile;
    public DamageDelivery Delivery;

    public int BaseDamage;
    public int FinalDamage;

    public bool IsCancelled;

    public DamageContext(BattleUnitInstance attacker, BattleUnitInstance target, DamageProfile profile, int baseDamage, DamageDelivery delivery)
    {
        Attacker = attacker;
        Target = target;
        Profile = profile;
        BaseDamage = baseDamage;
        FinalDamage = baseDamage;
        Delivery = delivery;
        IsCancelled = false;
    }
}

public abstract class BattleEffectRuntime
{
    public string EffectId { get; private set; }
    public BattleEffectSource Source { get; private set; }
    public BattleUnitInstance Owner { get; private set; }

    protected readonly Dictionary<string, float> Cooldowns = new();

    private readonly List<string> cooldownKeysBuffer = new();
    private readonly List<string> expiredCooldownKeysBuffer = new();

    public void Initialize(string effectId, BattleEffectSource source, BattleUnitInstance owner)
    {
        EffectId = effectId;
        Source = source;
        Owner = owner;
    }

    protected bool IsOnCooldown(string key)
    {
        return Cooldowns.ContainsKey(key);
    }

    protected void StartCooldown(string key, float duration)
    {
        Cooldowns[key] = duration;
    }

    protected void TickCooldowns(float deltaTime)
    {
        cooldownKeysBuffer.Clear();
        expiredCooldownKeysBuffer.Clear();

        foreach (string key in Cooldowns.Keys)
        {
            cooldownKeysBuffer.Add(key);
        }

        foreach (string key in cooldownKeysBuffer)
        {
            float remaining = Cooldowns[key] - deltaTime;

            if (remaining <= 0f)
            {
                expiredCooldownKeysBuffer.Add(key);
            }
            else
            {
                Cooldowns[key] = remaining;
            }
        }

        foreach (string key in expiredCooldownKeysBuffer)
        {
            Cooldowns.Remove(key);
        }
    }

    public virtual void OnBattleStart(BattleSystem battleSystem) { }

    public virtual void OnTick(BattleSystem battleSystem, float deltaTime) { }

    public virtual void OnBeforeApplyStatus(BattleSystem battleSystem, BattleStatusRuntime status) { }

    public virtual void OnBeforeReceiveDamage(BattleSystem battleSystem, DamageContext context) { }

    public virtual void OnAfterReceiveDamage(BattleSystem battleSystem, DamageContext context) { }

    public virtual void OnBasicAttackHit(BattleSystem battleSystem, DamageContext context) { }

    public virtual void OnUnitDeath(BattleSystem battleSystem, BattleUnitInstance deadUnit, BattleUnitInstance killer) { }

    public virtual void OnTargetQuery(BattleSystem battleSystem, TargetQueryContext context) { }
}

public sealed class BattleEffectSystem
{
    private BattleSystem BattleSystem;
    private BattleEffectFactory Factory;

    public void Initialize(BattleSystem battleSystem)
    {
        BattleSystem = battleSystem;
        Factory = new BattleEffectFactory();
    }

    public void BuildEffectsForBattle()
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            unit.Effects.Clear();

            BuildMutationEffects(unit);
            BuildInnateKeywordEffects(unit);
        }

        foreach (var unit in BattleSystem.BattleState.Units)
        {
            foreach (var effect in unit.Effects)
            {
                effect.OnBattleStart(BattleSystem);
            }
        }
    }

    private void BuildMutationEffects(BattleUnitInstance unit)
    {
        foreach (string mutationId in unit.MutationIds)
        {
            BattleEffectRuntime effect = Factory.Create(mutationId, BattleEffectSource.Mutation, unit);

            if (effect != null)
            {
                unit.Effects.Add(effect);
            }
        }
    }

    private void BuildInnateKeywordEffects(BattleUnitInstance unit)
    {
        UnitDefinition definition = BattleSystem.unitDatabase.GetUnit(unit.DefinitionId);

        if (definition.InnateKeywordIds == null)
        {
            return;
        }

        foreach (string keywordId in definition.InnateKeywordIds)
        {
            BattleEffectRuntime effect = Factory.Create(keywordId, BattleEffectSource.InnateKeyword, unit);

            if (effect != null)
            {
                unit.Effects.Add(effect);
            }
        }
    }

    public void Tick(float deltaTime)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            foreach (var effect in unit.Effects)
            {
                effect.OnTick(BattleSystem, deltaTime);
            }
        }
    }

    public void BeforeApplyStatus(BattleStatusRuntime status)
    {
        foreach (var effect in status.TargetUnit.Effects)
        {
            effect.OnBeforeApplyStatus(BattleSystem, status);
        }
    }

    public void BeforeReceiveDamage(DamageContext context)
    {
        foreach (var effect in context.Target.Effects)
        {
            effect.OnBeforeReceiveDamage(BattleSystem, context);
        }
    }

    public void AfterReceiveDamage(DamageContext context)
    {
        foreach (var effect in context.Target.Effects)
        {
            effect.OnAfterReceiveDamage(BattleSystem, context);
        }
    }

    public void BasicAttackHit(DamageContext context)
    {
        foreach (var effect in context.Attacker.Effects)
        {
            effect.OnBasicAttackHit(BattleSystem, context);
        }
    }

    public void UnitDeath(BattleUnitInstance deadUnit, BattleUnitInstance killer)
    {
        foreach (var unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            foreach (var effect in unit.Effects)
            {
                effect.OnUnitDeath(BattleSystem, deadUnit, killer);
            }
        }
    }

    public void TargetQuery(TargetQueryContext context)
    {
        foreach (var effect in context.Seeker.Effects)
        {
            effect.OnTargetQuery(BattleSystem, context);
        }
    }

    public void Clear()
    {

    }
}

public sealed class BattleEffectFactory
{
    private readonly Dictionary<string, Func<BattleEffectRuntime>> creators = new();

    public BattleEffectFactory()
    {
        Register("Carapace", () => new CarapaceEffectRuntime());
        Register("Mucus", () => new MucusEffectRuntime());
        Register("Mandibule", () => new MandibuleEffectRuntime());
        Register("Venin", () => new VeninEffectRuntime());
        Register("Anticorps", () => new AnticorpsEffectRuntime());
        Register("Camouflage", () => new CamouflageEffectRuntime());
        Register("Echolocation", () => new EcholocationEffectRuntime());
    }

    public void Register(string effectId, Func<BattleEffectRuntime> creator)
    {
        creators[effectId] = creator;
    }

    public BattleEffectRuntime Create(string effectId, BattleEffectSource source, BattleUnitInstance owner)
    {
        if (!creators.TryGetValue(effectId, out Func<BattleEffectRuntime> creator))
        {
            return null;
        }

        BattleEffectRuntime effect = creator();

        effect.Initialize(effectId, source, owner);

        return effect;
    }
}

public sealed class CarapaceEffectRuntime : BattleEffectRuntime
{
    private const int FlatDamageReduction = 5;

    public override void OnBeforeReceiveDamage(BattleSystem battleSystem, DamageContext context)
    {
        if (context.Target != Owner)
        {
            return;
        }

        if (context.Delivery == DamageDelivery.DamageOverTime)
        {
            return;
        }

        context.FinalDamage = Mathf.Max(0, context.FinalDamage - FlatDamageReduction);
    }
}

public sealed class MucusEffectRuntime : BattleEffectRuntime
{
    private const float SlowDuration = 2.5f;
    private const float AttackSpeedMultiplier = 0.70f;

    public override void OnBasicAttackHit(BattleSystem battleSystem, DamageContext context)
    {
        if (context.Attacker != Owner)
        {
            return;
        }

        GenericStatusRuntime slow = new();

        slow.Initialize(BattleStatusId.Mucus, context.Attacker, context.Target, SlowDuration);

        slow.AddModifier(BattleStatType.AttackSpeed, BattleStatModifierType.Multiply, AttackSpeedMultiplier);

        battleSystem.StatusSystem.ApplyStatus(slow);
    }
}

public sealed class MandibuleEffectRuntime : BattleEffectRuntime
{
    private const float Cooldown = 5f;
    private const float Duration = 2.5f;
    private const float ValueOverride = 0f;
    private const float ProcChance = 0.15f;

    public override void OnTick(BattleSystem battleSystem, float deltaTime)
    {
        TickCooldowns(deltaTime);
    }

    public override void OnBasicAttackHit(BattleSystem battleSystem, DamageContext context)
    {
        if (context.Attacker != Owner)
        {
            return;
        }

        string targetCooldownKey = context.Target.BattleInstanceId;

        if (IsOnCooldown(targetCooldownKey))
        {
            return;
        }

        if (UnityEngine.Random.value > ProcChance)
        {
            return;
        }

        StartCooldown(targetCooldownKey, Cooldown);

        GenericStatusRuntime stun = new();

        stun.Initialize(BattleStatusId.Stun, context.Attacker, context.Target, Duration);

        stun.AddModifier(BattleStatType.AttackSpeed, BattleStatModifierType.Override, ValueOverride);
        stun.AddModifier(BattleStatType.MoveSpeed, BattleStatModifierType.Override, ValueOverride);

        battleSystem.StatusSystem.ApplyStatus(stun);
    }
}

public sealed class VeninEffectRuntime : BattleEffectRuntime
{
    private const float PoisonDuration = 4f;
    private const int StackPerHit = 1;
    private const int MaxStacks = 5;
    private const int DamagePerStack = 2;
    private const float TickInterval = 1f;

    public override void OnBasicAttackHit(BattleSystem battleSystem, DamageContext context)
    {
        if (context.Attacker != Owner)
        {
            return;
        }

        PoisonStatusRuntime poison = new();

        poison.Initialize(BattleStatusId.Poison, context.Attacker, context.Target, PoisonDuration);

        poison.Setup(StackPerHit, MaxStacks, DamagePerStack, TickInterval);

        battleSystem.StatusSystem.ApplyStatus(poison);
    }
}

public sealed class AnticorpsEffectRuntime : BattleEffectRuntime
{
    public override void OnBeforeApplyStatus(BattleSystem battleSystem, BattleStatusRuntime status)
    {
        if (status.TargetUnit != Owner)
        {
            return;
        }

        if (status.StatusId != BattleStatusId.Poison)
        {
            return;
        }

        if (status is not PoisonStatusRuntime DoT)
        {
            return;
        }

        DoT.ClampStacks(1);
    }
}

public sealed class CamouflageEffectRuntime : BattleEffectRuntime
{
    private const float Cooldown = 6f;
    private const float Duration = 2.5f;

    private const string CooldownKey = "Camouflage";

    public override void OnTick(BattleSystem battleSystem, float deltaTime)
    {
        TickCooldowns(deltaTime);
    }

    public override void OnUnitDeath(BattleSystem battleSystem, BattleUnitInstance deadUnit, BattleUnitInstance killer)
    {
        if (killer != Owner)
        {
            return;
        }

        if (IsOnCooldown(CooldownKey))
        {
            return;
        }

        StartCooldown(CooldownKey, Cooldown);

        GenericStatusRuntime camouflage = new();

        camouflage.Initialize(BattleStatusId.Camouflage, Owner, Owner, Duration);

        battleSystem.StatusSystem.ApplyStatus(camouflage);

        ResetEnemyAggro(battleSystem);
    }

    private void ResetEnemyAggro(BattleSystem battleSystem)
    {
        foreach (var unit in battleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            if (unit.OwnerPlayerId == Owner.OwnerPlayerId)
            {
                continue;
            }

            if (unit.CurrentTargetBattleInstanceId != Owner.BattleInstanceId)
            {
                continue;
            }

            unit.CurrentTargetBattleInstanceId = null;
            unit.ResetNavigationState();

            unit.Decision = BattleUnitDecision.NoTarget;
            unit.DecisionReason = "Target lost due to Camouflage";
        }
    }
}

public sealed class EcholocationEffectRuntime : BattleEffectRuntime
{
    public override void OnTargetQuery(BattleSystem battleSystem, TargetQueryContext context)
    {
        if (context.Seeker != Owner)
        {
            return;
        }

        if (context.Candidate.IsCamouflaged())
        {
            context.CanTarget = true;
        }

        context.PriorityBonus += GetLowestHealthPriority(context.Candidate);
    }

    private int GetLowestHealthPriority(BattleUnitInstance candidate)
    {
        return candidate.CurrentHealth;
    }
}