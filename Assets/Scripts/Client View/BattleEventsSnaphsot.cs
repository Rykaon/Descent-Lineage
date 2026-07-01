using Unity.Collections;
using Unity.Netcode;

public struct BattleEventsSnapshot : INetworkSerializable
{
    public BattleDamageEventSnapshot[] DamageEvents;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        int damageCount = DamageEvents == null ? 0 : DamageEvents.Length;
        serializer.SerializeValue(ref damageCount);

        if (serializer.IsReader)
        {
            DamageEvents = new BattleDamageEventSnapshot[damageCount];
        }

        for (int i = 0; i < damageCount; i++)
        {
            serializer.SerializeValue(ref DamageEvents[i]);
        }
    }
}

public struct BattleDamageEventSnapshot : INetworkSerializable
{
    public FixedString64Bytes SourceBattleInstanceId;
    public FixedString64Bytes TargetBattleInstanceId;
    public int Amount;
    public DamageDelivery Delivery;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer)
        where T : IReaderWriter
    {
        serializer.SerializeValue(ref SourceBattleInstanceId);
        serializer.SerializeValue(ref TargetBattleInstanceId);
        serializer.SerializeValue(ref Amount);
        serializer.SerializeValue(ref Delivery);
    }
}