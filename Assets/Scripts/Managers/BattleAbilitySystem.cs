using UnityEngine;

public sealed class BattleAbilitySystem
{
    private BattleSystem BattleSystem;

    public void Initialize(BattleSystem battleSystem)
    {
        this.BattleSystem = battleSystem;
    }

    public void Tick(float deltaTime)
    {
        foreach (BattleUnitInstance unit in BattleSystem.BattleState.Units)
        {
            if (unit.IsDead)
            {
                continue;
            }

            if (unit.CurrentStats.ManaMax <= 0)
            {
                continue;
            }

            if (unit.CurrentMana < unit.CurrentStats.ManaMax)
            {
                continue;
            }

            BattleAbilityRuntime ability = BattleAbilityFactory.Create(unit.AbilityId);
            ability.Initialize(BattleSystem);

            if (!ability.CanCast(unit))
            {
                continue;
            }

            BattleUnitInstance target = BattleSystem.BattleState.GetUnitByBattleId(unit.CurrentTargetBattleInstanceId);

            if (target == null || target.IsDead)
            {
                continue;
            }

            int beforeMana = unit.CurrentMana;

            ability.Cast(unit, BattleSystem);

            int manaDelta = unit.CurrentMana - beforeMana;

            if (manaDelta != 0)
            {
                BattleSystem.EventBuffer.AddManaEvent(unit, manaDelta);
            }

            BattleSystem.CladeSystem.OnAbilityCast(unit);
        }
    }

    public void Clear()
    {

    }
}

public static class BattleAbilityFactory
{
    public static BattleAbilityRuntime Create(string abilityId)
    {
        return abilityId switch
        {
            "Morsure" => new HeavyBiteAbilityRuntime(),
            _ => new NoAbilityRuntime()
        };
    }
}

public sealed class HeavyBiteAbilityRuntime : BattleAbilityRuntime
{
    public override bool CanCast(BattleUnitInstance caster)
    {
        if (caster.IsDead)
        {
            return false;
        }

        if (string.IsNullOrEmpty(caster.CurrentTargetBattleInstanceId))
        {
            return false;
        }

        BattleUnitInstance target = battleSystem.BattleState.GetUnitByBattleId(caster.CurrentTargetBattleInstanceId);

        if (target == null || target.IsDead)
        {
            return false;
        }

        return true;
    }

    public override void Cast(BattleUnitInstance caster, BattleSystem battleSystem)
    {
        BattleUnitInstance target = battleSystem.BattleState.GetUnitByBattleId(caster.CurrentTargetBattleInstanceId);

        caster.CurrentMana = 0;

        int damage = caster.CurrentStats.SlashOffense * 1;

        DamageContext context = new DamageContext(caster, target, caster.AbilityDamageProfile, damage, DamageDelivery.Ability);

        battleSystem.EffectSystem.BeforeReceiveDamage(context);

        if (context.IsCancelled || context.FinalDamage <= 0)
        {
            return;
        }

        target.LastDamageSourceBattleInstanceId = caster.BattleInstanceId;
        target.PendingDamageContexts.Add(context);
    }
}