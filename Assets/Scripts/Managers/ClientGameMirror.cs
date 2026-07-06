using System.Collections.Generic;

public sealed class ClientGameMirror
{
    public GamePhase Phase;

    public int LocalPlayerId = -1;
    public int OpponentPlayerId = -1;

    public int PreparationRemainingTime = -1;

    public readonly ClientPlayerMirror[] Players =
    {
        new ClientPlayerMirror(0),
        new ClientPlayerMirror(1)
    };

    public readonly ClientSharedBoardMirror SharedBoard = new();

    public ClientPlayerMirror LocalPlayer => LocalPlayerId >= 0 ? Players[LocalPlayerId] : null;
    public ClientPlayerMirror OpponentPlayer => LocalPlayerId >= 0 ? Players[OpponentPlayerId] : null;

    public ClientPlayerMirror GetPlayer(int playerId)
    {
        return Players[playerId];
    }
}

public sealed class ClientPlayerMirror
{
    public int PlayerId;

    public int Life;
    public int Amber;
    public int BiomeCount;
    public int BoardCapacity;
    public int UnitsOnBoard;

    public readonly ClientBoardMirror Board = new();
    public readonly ClientShopMirror Shop = new();
    public readonly ClientFaunaShopMirror FaunaShop = new();
    public readonly ClientFossilMirror Fossil = new();

    public ClientPlayerMirror(int playerId)
    {
        PlayerId = playerId;
    }
}

public sealed class ClientBoardMirror
{
    public readonly Dictionary<string, ClientBoardUnitMirror> UnitsById = new();
    public readonly Dictionary<BoardNode, string> UnitIdByNode = new();

    public bool TryGetUnit(string unitId, out ClientBoardUnitMirror unit)
    {
        return UnitsById.TryGetValue(unitId, out unit);
    }
}

public sealed class ClientBoardUnitMirror
{
    public string InstanceId;
    public string DefinitionId;
    public int OwnerPlayerId;
    public BoardNode Node;
    public int MutationCount;

    public int SellCost => EconomySystem.GetUnitSellCostFromMutationCount(MutationCount);
}

public sealed class ClientSharedBoardMirror
{
    public readonly Dictionary<BoardNode, ClientBoardTileMirror> Tiles = new();

    public bool TryGetTile(BoardNode node, out ClientBoardTileMirror tile)
    {
        return Tiles.TryGetValue(node, out tile);
    }
}

public sealed class ClientBoardTileMirror
{
    public BoardNode Node;
    public int OwnerPlayerId;
    public BoardType BoardType;
    public BiomeType BiomeType;
}

public sealed class ClientShopMirror
{
    public ShopSlot[] Slots = new ShopSlot[5];
}

public sealed class ClientFaunaShopMirror
{
    public FaunaShopSlot[] Slots = new FaunaShopSlot[5];
}

public sealed class ClientFossilMirror
{
    public int PlayerId;
    public int FossilLevel;
    public int CurrentXp;
    public int NextLevelXp;
    public int XpToNextLevel;

    public readonly List<ClientFossilMutationMirror> Mutations = new();

    public void Clear()
    {
        PlayerId = -1;
        FossilLevel = 0;
        CurrentXp = 0;
        NextLevelXp = 0;
        XpToNextLevel = 0;
        Mutations.Clear();
    }
}

public sealed class ClientFossilMutationMirror
{
    public string MutationId;
    public string DisplayName;
    public BiomeType Biome;
    public int Rank;
}