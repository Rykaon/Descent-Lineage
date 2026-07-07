using Unity.Netcode;

public struct BattleInitSnapshot : INetworkSerializable
{
    public BattleInitUnitSnapshot[] Units;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        NetworkSerializationUtility.SerializeArray(serializer, ref Units);
    }
}

public struct BattleFrameSnapshot : INetworkSerializable
{
    public BattleFrameUnitSnapshot[] Units;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        NetworkSerializationUtility.SerializeArray(serializer, ref Units);
    }
}