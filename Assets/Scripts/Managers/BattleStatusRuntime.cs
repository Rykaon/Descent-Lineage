using System.Collections.Generic;

public enum BattleStatusId
{
    Poison,
    Grab,
    Stun,
    Fear,
    Root,
    Mucus,
    Venom,
    Burning,
    Bleeding,
    Camouflage
}

public abstract class BattleStatusRuntime
{
    public BattleStatusId StatusId { get; private set; }
    public BattleUnitInstance SourceUnit { get; private set; }
    public BattleUnitInstance TargetUnit { get; private set; }

    public float DurationRemaining { get; protected set; }
    public bool IsExpired => DurationRemaining <= 0f;

    public void Initialize(BattleStatusId statusId, BattleUnitInstance sourceUnit, BattleUnitInstance targetUnit, float duration)
    {
        StatusId = statusId;
        SourceUnit = sourceUnit;
        TargetUnit = targetUnit;
        DurationRemaining = duration;

        OnInitialize();
    }

    protected virtual void OnInitialize() { }

    public virtual void Tick(BattleSystem battleSystem, float deltaTime)
    {
        DurationRemaining -= deltaTime;
    }

    public virtual bool CanStackWith(BattleStatusRuntime other)
    {
        return true;
    }

    public virtual void RefreshFrom(BattleStatusRuntime other)
    {
        DurationRemaining = other.DurationRemaining;
    }

    public virtual void BuildModifiers(List<BattleStatModifier> buffer) { }

    public virtual void OnExpire(BattleSystem battleSystem) { }

    public virtual bool TryMergeWith(BattleStatusRuntime other)
    {
        return false;
    }
}