using Unity.Netcode;

public struct BattleInitSnapshot : INetworkSerializable
{
    public BattleInitUnitSnapshot[] Units;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = Units == null ? 0 : Units.Length;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
        {
            Units = new BattleInitUnitSnapshot[count];
        }

        for (int i = 0; i < count; i++)
        {
            Units[i].NetworkSerialize(serializer);
        }
    }
}

public struct BattleFrameSnapshot : INetworkSerializable
{
    public BattleFrameUnitSnapshot[] Units;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = Units == null ? 0 : Units.Length;
        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
        {
            Units = new BattleFrameUnitSnapshot[count];
        }

        for (int i = 0; i < count; i++)
        {
            Units[i].NetworkSerialize(serializer);
        }
    }
}