using Unity.Collections;
using Unity.Netcode;

public struct BattleInitUnitSnapshot : INetworkSerializable
{
    public int UnitIndex;

    public FixedString64Bytes BattleInstanceId;
    public FixedString64Bytes BoardInstanceId;
    public FixedString64Bytes DefinitionId;

    public int OwnerPlayerId;

    public int CurrentHexQ;
    public int CurrentHexR;

    public int CurrentHealth;
    public int MaxHealth;
    public int CurrentMana;
    public int MaxMana;

    public float AttackSpeed;
    public float MoveSpeed;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref UnitIndex);
        serializer.SerializeValue(ref BattleInstanceId);
        serializer.SerializeValue(ref BoardInstanceId);
        serializer.SerializeValue(ref DefinitionId);
        serializer.SerializeValue(ref OwnerPlayerId);
        serializer.SerializeValue(ref CurrentHexQ);
        serializer.SerializeValue(ref CurrentHexR);
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MaxHealth);
        serializer.SerializeValue(ref CurrentMana);
        serializer.SerializeValue(ref MaxMana);
        serializer.SerializeValue(ref AttackSpeed);
        serializer.SerializeValue(ref MoveSpeed);
    }
}

public struct BattleFrameUnitSnapshot : INetworkSerializable
{
    public int UnitIndex;

    public int CurrentHexQ;
    public int CurrentHexR;

    public int CurrentHealth;
    public int MaxHealth;
    public int CurrentMana;
    public int MaxMana;

    public bool IsDead;

    public float AttackSpeed;
    public float MoveSpeed;

    public int TargetUnitIndex;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref UnitIndex);
        serializer.SerializeValue(ref CurrentHexQ);
        serializer.SerializeValue(ref CurrentHexR);
        serializer.SerializeValue(ref CurrentHealth);
        serializer.SerializeValue(ref MaxHealth);
        serializer.SerializeValue(ref CurrentMana);
        serializer.SerializeValue(ref MaxMana);
        serializer.SerializeValue(ref IsDead);
        serializer.SerializeValue(ref AttackSpeed);
        serializer.SerializeValue(ref MoveSpeed);
        serializer.SerializeValue(ref TargetUnitIndex);
    }
}