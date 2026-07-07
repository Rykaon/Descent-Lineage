using Unity.Collections;
using Unity.Netcode;

public struct BattleEventsSnapshot : INetworkSerializable
{
    public BattleDamageEventSnapshot[] DamageEvents;
    public BattleManaEventSnapshot[] ManaEvents;
    public BattleHealEventSnapshot[] HealEvents;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        NetworkSerializationUtility.SerializeArray(serializer, ref DamageEvents);
        NetworkSerializationUtility.SerializeArray(serializer, ref ManaEvents);
        NetworkSerializationUtility.SerializeArray(serializer, ref HealEvents);
    }
}

public struct BattleDamageEventSnapshot : INetworkSerializable
{
    public FixedString64Bytes SourceBattleInstanceId;
    public FixedString64Bytes TargetBattleInstanceId;
    public int Amount;
    public DamageDelivery Delivery;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref SourceBattleInstanceId);
        serializer.SerializeValue(ref TargetBattleInstanceId);
        serializer.SerializeValue(ref Amount);
        serializer.SerializeValue(ref Delivery);
    }
}

public struct BattleManaEventSnapshot : INetworkSerializable
{
    public FixedString64Bytes TargetBattleInstanceId;
    public int CurrentMana;
    public int MaxMana;
    public int Amount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TargetBattleInstanceId);
        serializer.SerializeValue(ref CurrentMana);
        serializer.SerializeValue(ref MaxMana);
        serializer.SerializeValue(ref Amount);
    }
}

public struct BattleHealEventSnapshot : INetworkSerializable
{
    public FixedString64Bytes TargetBattleInstanceId;
    public int Amount;
    public int CurrentHealth;
    public int MaxHealth;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref TargetBattleInstanceId);
        serializer.SerializeValue(ref Amount);
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MaxHealth);
    }
}