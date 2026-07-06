using Unity.Collections;
using Unity.Netcode;

public struct GamePhaseSnapshot : INetworkSerializable
{
    public GamePhase Phase;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Phase);
    }
}

public struct BoardUnitSnapshot : INetworkSerializable
{
    public FixedString64Bytes InstanceId;
    public FixedString64Bytes DefinitionId;
    public int OwnerPlayerId;
    public BoardNode Node;
    public int MutationCount;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref InstanceId);
        serializer.SerializeValue(ref DefinitionId);
        serializer.SerializeValue(ref OwnerPlayerId);
        serializer.SerializeValue(ref Node);
        serializer.SerializeValue(ref MutationCount);
    }
}

public struct PlayerStateSnapshot : INetworkSerializable
{
    public int PlayerId;

    public int Life;
    public int Amber;
    public int BiomeBudget;
    public int BoardCapacity;
    public int UnitsOnBoard;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref Life);
        serializer.SerializeValue(ref Amber);
        serializer.SerializeValue(ref BiomeBudget);
        serializer.SerializeValue(ref BoardCapacity);
        serializer.SerializeValue(ref UnitsOnBoard);
    }
}

public struct ShopSlotSnapshot : INetworkSerializable
{
    public int PlayerId;
    public int SlotIndex;

    public FixedString64Bytes UnitDefinitionId;
    public FixedString64Bytes MutationId;

    public int Cost;
    public bool IsEmpty;
    public bool IsBrick;
    public bool IsMutationUpgrade;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref SlotIndex);
        serializer.SerializeValue(ref UnitDefinitionId);
        serializer.SerializeValue(ref MutationId);
        serializer.SerializeValue(ref Cost);
        serializer.SerializeValue(ref IsEmpty);
        serializer.SerializeValue(ref IsBrick);
        serializer.SerializeValue(ref IsMutationUpgrade);
    }
}

public struct FaunaShopSlotSnapshot : INetworkSerializable
{
    public int PlayerId;
    public int SlotIndex;

    public FixedString64Bytes FaunaDefinitionId;
    public FixedString64Bytes MutationId;

    public int FossileValue;
    public int Cost;
    public bool IsBrick;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref SlotIndex);
        serializer.SerializeValue(ref FaunaDefinitionId);
        serializer.SerializeValue(ref MutationId);
        serializer.SerializeValue(ref FossileValue);
        serializer.SerializeValue(ref Cost);
        serializer.SerializeValue(ref IsBrick);
    }
}

public struct ShopStateSnapshot : INetworkSerializable
{
    public ShopSlotSnapshot[] Slots;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = Slots == null ? 0 : Slots.Length;

        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
        {
            Slots = new ShopSlotSnapshot[count];
        }

        for (int i = 0; i < count; i++)
        {
            serializer.SerializeValue(ref Slots[i]);
        }
    }
}

public struct FaunaShopStateSnapshot : INetworkSerializable
{
    public FaunaShopSlotSnapshot[] Slots;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        int count = Slots == null ? 0 : Slots.Length;

        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
        {
            Slots = new FaunaShopSlotSnapshot[count];
        }

        for (int i = 0; i < count; i++)
        {
            serializer.SerializeValue(ref Slots[i]);
        }
    }
}

public struct BoardTileSnapshot : INetworkSerializable
{
    public BoardNode Node;
    public int OwnerPlayerId;
    public BoardType BoardType;
    public BiomeType BiomeType;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref Node);
        serializer.SerializeValue(ref OwnerPlayerId);
        serializer.SerializeValue(ref BoardType);
        serializer.SerializeValue(ref BiomeType);
    }
}

public struct BoardStateSnapshot : INetworkSerializable
{
    public BoardUnitSnapshot[] Units;
    public BoardTileSnapshot[] Tiles;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        SerializeArray(serializer, ref Units);
        SerializeArray(serializer, ref Tiles);
    }

    private static void SerializeArray<TSerializer, TElement>(BufferSerializer<TSerializer> serializer, ref TElement[] array)
        where TSerializer : IReaderWriter
        where TElement : INetworkSerializable, new()
    {
        int count = array == null ? 0 : array.Length;

        serializer.SerializeValue(ref count);

        if (serializer.IsReader)
        {
            array = new TElement[count];
        }

        for (int i = 0; i < count; i++)
        {
            serializer.SerializeValue(ref array[i]);
        }
    }
}

public struct FossilStateSnapshot : INetworkSerializable
{
    public int PlayerId;
    public int FossilLevel;
    public int CurrentXp;
    public int NextLevelXp;
    public int XpToNextLevel;

    public FossilMutationSnapshot[] Mutations;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref PlayerId);
        serializer.SerializeValue(ref FossilLevel);
        serializer.SerializeValue(ref CurrentXp);
        serializer.SerializeValue(ref NextLevelXp);
        serializer.SerializeValue(ref XpToNextLevel);
        serializer.SerializeValue(ref Mutations);
    }
}

public struct FossilMutationSnapshot : INetworkSerializable
{
    public FixedString64Bytes MutationId;
    public FixedString64Bytes DisplayName;
    public BiomeType Biome;
    public int Rank;

    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref MutationId);
        serializer.SerializeValue(ref DisplayName);
        serializer.SerializeValue(ref Biome);
        serializer.SerializeValue(ref Rank);
    }
}