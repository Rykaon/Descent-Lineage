using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class ShopSystem
{
    private ShopPool Pool;
    private IUnitDefinitionDatabase unitDatabase;
    private IMutationDefinitionDatabase mutationDatabase;
    private SharedBoardState SharedBoard;

    public void Initialize(IUnitDefinitionDatabase unitDatabase, IMutationDefinitionDatabase mutationDatabase, SharedBoardState sharedBoard)
    {
        Pool = new ShopPool();

        this.unitDatabase = unitDatabase;
        this.mutationDatabase = mutationDatabase;
        SharedBoard = sharedBoard;

        FillPool();
    }

    public void FillPool()
    {
        foreach (UnitDefinition unit in unitDatabase.GetAllUnits())
        {
            Pool.RegisterToPool(unit);
        }
    }

    public void FillAllShopsRoundRobin(GameState state, System.Random rng)
    {
        foreach (var player in state.Players)
        {
            ReturnUnboughtShopUnitsToPool(player);
        }

        int shopSize = 5;
        int firstPlayerIndex = state.RoundIndex % state.Players.Length;

        for (int slotIndex = 0; slotIndex < shopSize; slotIndex++)
        {
            for (int offset = 0; offset < state.Players.Length; offset++)
            {
                int playerIndex = (firstPlayerIndex + offset) % state.Players.Length;
                PlayerState player = state.Players[playerIndex];

                ShopSlot slot = RollAndReserveShopSlot(player, rng);
                player.Shop.Slots[slotIndex] = slot;
            }
        }
    }

    private ShopSlot RollAndReserveShopSlot(PlayerState player, System.Random rng)
    {
        List<PoolEntry> candidates = GatherShopCandidates(player);

        if (candidates.Count == 0)
        {
            return ShopSlot.CreateBrick();
        }

        PoolEntry rolledEntry = RollEntryWeightedByRemainingCount(candidates, rng);

        if (rolledEntry == null)
        {
            return ShopSlot.CreateBrick();
        }

        Pool.ReserveCopy(rolledEntry.UnitDefinitionId);

        if (!player.Board.TryGetUnitByDefinitionId(rolledEntry.UnitDefinitionId, out var ownedUnit))
        {
            return ShopSlot.CreateNewCreature(
                rolledEntry.UnitDefinitionId,
                cost: 1
            );
        }

        List<string> eligibleMutations = GetEligibleMutationsForOwnedUnit(player, ownedUnit);

        if (eligibleMutations.Count == 0)
        {
            return ShopSlot.CreateBrickReservedCreature(rolledEntry.UnitDefinitionId);
        }

        List<string> mutationsId = new List<string>(ownedUnit.MutationIds);
        mutationsId.Add(eligibleMutations[rng.Next(0, eligibleMutations.Count)]);

        return ShopSlot.CreateMutationUpgrade(rolledEntry.UnitDefinitionId, mutationsId, cost: GetCostForMutationCount(mutationsId.Count)
        );
    }

    private PoolEntry RollEntryWeightedByRemainingCount(List<PoolEntry> entries, System.Random rng)
    {
        int totalWeight = 0;

        foreach (PoolEntry entry in entries)
        {
            totalWeight += entry.RemainingCount;
        }

        if (totalWeight == 0)
        {
            return null;
        }

        int roll = rng.Next(totalWeight);
        int cumulative = 0;

        foreach (PoolEntry entry in entries)
        {
            cumulative += entry.RemainingCount;

            if (roll < cumulative)
            {
                return entry;
            }
        }

        return null;
    }

    private List<PoolEntry> GatherShopCandidates(PlayerState player)
    {
        List<PoolEntry> candidates = new();

        foreach (PoolEntry entry in Pool.GetAllEntries())
        {
            if (!Pool.HasAvailableCopy(entry.UnitDefinitionId))
            {
                continue;
            }

            if (player.Board.TryGetUnitByDefinitionId(entry.UnitDefinitionId, out var ownedUnit) && ownedUnit.MutationIds.Count >= 3)
            {
                continue;
            }

            candidates.Add(entry);
        }

        return candidates;
    }

    private List<string> GetEligibleMutationsForOwnedUnit(PlayerState player, BoardUnitInstance unit)
    {
        var result = new List<string>();

        BiomeType currentBiome = SharedBoard.GetBiomeTypeAt(unit.Node);

        foreach (string mutation in player.Fossil.Mutations.Keys.ToList())
        {
            if (unit.MutationIds.Contains(mutation))
                continue;

            UnitDefinition unitDefinition = unitDatabase.GetUnit(unit.DefinitionId);

            if (!unitDefinition.EligibleMutationIds.Contains(mutation))
                continue;

            MutationDefinition mutationDefinition = mutationDatabase.GetMutation(mutation);

            if (!mutationDefinition.EligibleBiomes.Contains(currentBiome))
                continue;

            result.Add(mutation);
        }

        return result;
    }

    private int GetCostForMutationCount(int count)
    {
        return count switch
        {
            0 => 1,
            1 => 3,
            2 => 6,
            3 => 9,
            _ => 999
        };
    }

    public void UpdateShopSlotsAfterPurchase(ShopSlot purchasedSlot, ShopState shop)
    {
        if (purchasedSlot == null)
            return;

        if (purchasedSlot.MutationsId == null)
            purchasedSlot.MutationsId = new List<string>();

        string purchasedUnitId = purchasedSlot.UnitDefinitionId;
        int purchasedMutationCount = purchasedSlot.MutationsId.Count;

        string purchasedAddedMutationId = purchasedMutationCount > 0
            ? purchasedSlot.MutationsId[purchasedMutationCount - 1]
            : null;

        foreach (ShopSlot slot in shop.Slots)
        {
            if (slot == null)
                continue;

            if (slot == purchasedSlot)
                continue;

            if (slot.UnitDefinitionId != purchasedUnitId)
                continue;

            if (slot.MutationsId == null)
                slot.MutationsId = new List<string>();

            if (purchasedMutationCount >= 3)
            {
                BrickSlot(slot);
                continue;
            }

            if (string.IsNullOrEmpty(purchasedAddedMutationId))
            {
                BrickSlot(slot);
                continue;
            }

            if (slot.MutationsId.Count == 0)
            {
                BrickSlot(slot);
                continue;
            }

            string slotOfferedMutationId = slot.MutationsId[slot.MutationsId.Count - 1];

            if (slotOfferedMutationId == purchasedAddedMutationId)
            {
                BrickSlot(slot);
                continue;
            }

            if (purchasedSlot.MutationsId.Contains(slotOfferedMutationId))
            {
                BrickSlot(slot);
                continue;
            }

            slot.MutationsId.Insert(slot.MutationsId.Count - 1, purchasedAddedMutationId);

            if (slot.MutationsId.Count > 3)
            {
                BrickSlot(slot);
                continue;
            }

            slot.Cost = GetCostForMutationCount(slot.MutationsId.Count);
        }
    }

    private void BrickSlot(ShopSlot slot)
    {
        slot.IsBrick = true;
        slot.Cost = 0;
    }

    public void RefreshPlayerShop(PlayerState player, System.Random rng)
    {
        ReturnUnboughtShopUnitsToPool(player);
        player.AmberCount -= player.Shop.RefreshCost;

        int shopSize = 5;

        for (int slotIndex = 0; slotIndex < shopSize; slotIndex++)
        {
            ShopSlot slot = RollAndReserveShopSlot(player, rng);
            player.Shop.Slots[slotIndex] = slot;
        }
    }

    private void ReturnUnboughtShopUnitsToPool(PlayerState player)
    {
        foreach (ShopSlot slot in player.Shop.Slots)
        {
            if (slot == null)
                continue;

            if (string.IsNullOrEmpty(slot.UnitDefinitionId))
                continue;

            Pool.ReturnCopy(slot.UnitDefinitionId);
        }

        Array.Clear(player.Shop.Slots, 0, 5);
    }

    public bool TryBuySlot(PlayerState player, int slotIndex, GamePhase phase)
    {
        if (player.AmberCount < player.Shop.Slots[slotIndex].Cost)
        {
            return false;
        }

        player.Board.TryGetUnitByDefinitionId(player.Shop.Slots[slotIndex].UnitDefinitionId, out BoardUnitInstance unit);

        if (unit == null)
        {
            if (phase == GamePhase.Preparation)
            {
                SharedBoard.TryGetFirstFreeTile(player, BoardType.Bench, out BoardTileState benchTile);

                if (benchTile == null)
                {
                    if (player.Board.UnitsOnBoard < player.Board.BoardCapacity)
                    {
                        SharedBoard.TryGetFirstFreeTile(player, BoardType.Board, out BoardTileState boardTile);

                        if (boardTile == null)
                        {
                            Debug.Log("No free tile available.");
                            return false;
                        }
                        else
                        {
                            BoardUnitInstance newUnit = CreateBoardUnitFromShopSlot(player, boardTile.Node, player.Shop.Slots[slotIndex]);
                            player.Board.RegisterUnit(newUnit, boardTile);
                        }
                    }
                    else
                    {
                        Debug.Log("Bench is full and too many units on Board.");
                        return false;
                    }
                }
                else
                {
                    BoardUnitInstance newUnit = CreateBoardUnitFromShopSlot(player, benchTile.Node, player.Shop.Slots[slotIndex]);
                    player.Board.RegisterUnit(newUnit, benchTile);
                }
            }
            else
            {
                SharedBoard.TryGetFirstFreeTile(player, BoardType.Bench, out BoardTileState BenchTile);

                if (BenchTile == null)
                {
                    Debug.Log("Bench is full.");
                    return false;
                }
                else
                {
                    BoardUnitInstance newUnit = CreateBoardUnitFromShopSlot(player, BenchTile.Node, player.Shop.Slots[slotIndex]);
                    player.Board.RegisterUnit(newUnit, BenchTile);
                }
            }
        }
        else
        {
            if (unit.MutationIds.Count < player.Shop.Slots[slotIndex].MutationsId.Count)
            {
                int lastIndex = player.Shop.Slots[slotIndex].MutationsId.Count - 1;
                unit.MutationIds.Add(player.Shop.Slots[slotIndex].MutationsId[lastIndex]);
            }
        }

        player.AmberCount -= player.Shop.Slots[slotIndex].Cost;
        return true;
    }

    private BoardUnitInstance CreateBoardUnitFromShopSlot(PlayerState player, BoardNode node, ShopSlot slot)
    {
        BoardUnitInstance unit = new BoardUnitInstance(slot.UnitDefinitionId, player.PlayerId, node);

        return unit;
    }
}

public class ShopPool
{
    private readonly Dictionary<string, PoolEntry> entriesByUnitId = new();

    public void RegisterToPool(UnitDefinition unit)
    {
        entriesByUnitId[unit.Id] = new PoolEntry(unit.Id, unit.MaxPoolCount);
    }

    public bool TryGetEntry(string unitDefinitionId, out PoolEntry entry)
    {
        return entriesByUnitId.TryGetValue(unitDefinitionId, out entry);
    }

    public bool HasAvailableCopy(string unitDefinitionId)
    {
        return TryGetEntry(unitDefinitionId, out var entry)
            && entry.RemainingCount > 0;
    }

    public bool ReserveCopy(string unitDefinitionId)
    {
        if (!TryGetEntry(unitDefinitionId, out var entry))
            return false;

        if (entry.RemainingCount <= 0)
            return false;

        entry.RemainingCount--;
        return true;
    }

    public bool ReturnCopy(string unitDefinitionId)
    {
        if (!TryGetEntry(unitDefinitionId, out var entry))
            return false;

        if (entry.RemainingCount >= entry.MaxPoolCount)
            return false;

        entry.RemainingCount++;
        return true;
    }

    public IEnumerable<PoolEntry> GetAllEntries()
    {
        return entriesByUnitId.Values;
    }
}

public class PoolEntry
{
    public string UnitDefinitionId;
    public int MaxPoolCount;
    public int RemainingCount;

    public PoolEntry(string unitDefinitionId, int maxPoolCount)
    {
        UnitDefinitionId = unitDefinitionId;
        MaxPoolCount = maxPoolCount;
        RemainingCount = maxPoolCount;
    }
}