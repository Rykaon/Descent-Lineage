using UnityEngine;

public abstract class BattleAbilityRuntime
{
    protected BattleSystem battleSystem;

    public virtual void Initialize(BattleSystem battleSystem)
    {
        this.battleSystem = battleSystem;
    }

    public abstract bool CanCast(BattleUnitInstance caster);
    public abstract void Cast(BattleUnitInstance caster, BattleSystem battleSystem);
}

public sealed class NoAbilityRuntime : BattleAbilityRuntime
{
    public override bool CanCast(BattleUnitInstance caster)
    {
        return false;
    }

    public override void Cast(BattleUnitInstance caster, BattleSystem battleSystem) { }
}