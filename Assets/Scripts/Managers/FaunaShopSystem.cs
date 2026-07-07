using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class FaunaShopSystem
{
    private FaunaShopPool Pool;
    private IFaunaDefinitionDatabase faunaDatabase;
    private IMutationDefinitionDatabase mutationDatabase;
    private SharedBoardState SharedBoard;

    public void Initialize(IFaunaDefinitionDatabase faunaDatabase, IMutationDefinitionDatabase mutationDatabase, SharedBoardState sharedBoard)
    {
        Pool = new FaunaShopPool();

        this.faunaDatabase = faunaDatabase;
        this.mutationDatabase = mutationDatabase;
        SharedBoard = sharedBoard;

        FillPool();
    }

    public void FillPool()
    {
        foreach (FaunaDefinition fauna in faunaDatabase.GetAllFaunas())
        {
            Pool.RegisterToPool(fauna);
        }
    }

    public void FillAllShopsRoundRobin(GameState state, System.Random rng)
    {
        foreach (var player in state.Players)
        {
            ReturnUnboughtShopFaunasToPool(player);
        }

        int shopSize = 3;
        int firstPlayerIndex = state.RoundIndex % state.Players.Length;

        for (int slotIndex = 0; slotIndex < shopSize; slotIndex++)
        {
            for (int offset = 0; offset < state.Players.Length; offset++)
            {
                int playerIndex = (firstPlayerIndex + offset) % state.Players.Length;
                PlayerState player = state.Players[playerIndex];

                FaunaShopSlot slot = RollAndReserveShopSlot(player, rng);
                player.FaunaShop.Slots[slotIndex] = slot;
            }
        }
    }

    private FaunaShopSlot RollAndReserveShopSlot(PlayerState player, System.Random rng)
    {
        BiomeType rolledBiome = RollAvailableBiomeForPlayer(player, rng);

        if (rolledBiome == BiomeType.None)
        {
            return FaunaShopSlot.CreateBrick();
        }

        if (!Pool.TryGetEntriesByBiome(rolledBiome, out List<FaunaPoolEntry> entries))
        {
            return FaunaShopSlot.CreateBrick();
        }

        FaunaPoolEntry rolledEntry = RollEntryWeightedByRemainingCount(entries, rng);

        if (rolledEntry == null)
        {
            return FaunaShopSlot.CreateBrick();
        }

        if (!Pool.ReserveCopy(rolledEntry.FaunaDefinitionId))
        {
            return FaunaShopSlot.CreateBrick();
        }

        FaunaDefinition rolledFauna = faunaDatabase.GetFauna(rolledEntry.FaunaDefinitionId);

        /*if (rolledFauna.EligibleMutations == null || rolledFauna.EligibleMutations.Count == 0)
        {
            return FaunaShopSlot.CreateBrick();
        }*/

        MutationDefinition rolledMutation = rolledFauna.EligibleMutations[rng.Next(0, rolledFauna.EligibleMutations.Count)];

        return FaunaShopSlot.CreateShopSlot(rolledEntry.FaunaDefinitionId, rolledMutation.Id, rolledFauna.FossilValue, rolledFauna.Cost
        );
    }

    private BiomeType RollAvailableBiomeForPlayer(PlayerState player, System.Random rng)
    {
        List<BiomeType> biomes = new();

        foreach (BoardTileState tile in SharedBoard.GetTilesForPlayerOrdered(player, BoardType.Board))
        {
            if (tile.Biome == BiomeType.None)
            {
                continue;
            }

            if (!biomes.Contains(tile.Biome))
            {
                biomes.Add(tile.Biome);
            }
        }

        if (biomes.Count == 0)
        {
            return BiomeType.None;
        }

        List<BiomeType> validBiomes = new();

        foreach (BiomeType biome in biomes)
        {
            if (!Pool.TryGetEntriesByBiome(biome, out var entries))
            {
                continue;
            }

            foreach (FaunaPoolEntry entry in entries)
            {
                if (entry.RemainingCount > 0)
                {
                    validBiomes.Add(biome);
                    break;
                }
            }
        }

        if (validBiomes.Count == 0)
        {
            return BiomeType.None;
        }

        return validBiomes[rng.Next(0, validBiomes.Count)];
    }

    private FaunaPoolEntry RollEntryWeightedByRemainingCount(List<FaunaPoolEntry> entries, System.Random rng)
    {
        int totalWeight = 0;

        foreach (var entry in entries)
        {
            if (entry.RemainingCount > 0)
            {
                totalWeight += entry.RemainingCount;
            }
        }

        if (totalWeight <= 0)
        {
            return null;
        }

        int roll = rng.Next(0, totalWeight);
        int cumulative = 0;

        foreach (var entry in entries)
        {
            if (entry.RemainingCount <= 0)
            {
                continue;
            }

            cumulative += entry.RemainingCount;

            if (roll < cumulative)
            {
                return entry;
            }
        }

        return null;
    }

    private void ReturnUnboughtShopFaunasToPool(PlayerState player)
    {
        foreach (FaunaShopSlot slot in player.FaunaShop.Slots)
        {
            if (slot == null)
                continue;

            if (string.IsNullOrEmpty(slot.FaunaDefinitionId))
                continue;

            Pool.ReturnCopy(slot.FaunaDefinitionId);
        }

        Array.Clear(player.FaunaShop.Slots, 0, 3);
    }

    public void RefreshPlayerShop(PlayerState player, System.Random rng)
    {
        ReturnUnboughtShopFaunasToPool(player);

        int shopSize = 3;

        for (int slotIndex = 0; slotIndex < shopSize; slotIndex++)
        {
            FaunaShopSlot slot = RollAndReserveShopSlot(player, rng);
            player.FaunaShop.Slots[slotIndex] = slot;
        }
    }

    public bool TryBuySlot(PlayerState player, int slotIndex, GamePhase phase)
    {
        if (player.FaunaShop.Slots[slotIndex].IsBrick)
        {
            return false;
        }

        if (player.AmberCount < player.FaunaShop.Slots[slotIndex].Cost)
        {
            return false;
        }
        
        player.AmberCount -= player.FaunaShop.Slots[slotIndex].Cost;
        player.Fossil.FossilValue += player.FaunaShop.Slots[slotIndex].FossilValue;

        if (player.Fossil.Mutations.TryGetValue(player.FaunaShop.Slots[slotIndex].MutationId, out var mutationCount))
        {
            player.Fossil.Mutations[player.FaunaShop.Slots[slotIndex].MutationId] = mutationCount++;
        }
        else
        {
            player.Fossil.Mutations.Add(player.FaunaShop.Slots[slotIndex].MutationId, 1);
        }

        return true;
    }
}

public class FaunaShopPool
{
    private readonly Dictionary<string, FaunaPoolEntry> entriesByFaunaId = new();

    private readonly Dictionary<BiomeType, List<FaunaPoolEntry>> entriesByBiome = new();

    public void RegisterToPool(FaunaDefinition fauna)
    {
        FaunaPoolEntry entry = new FaunaPoolEntry(fauna.Id, fauna.MaxPoolCount);

        entriesByFaunaId[fauna.Id] = entry;

        foreach (BiomeType biome in fauna.EligibleBiomes)
        {
            if (!entriesByBiome.TryGetValue(biome, out var list))
            {
                list = new List<FaunaPoolEntry>();
                entriesByBiome[biome] = list;
            }

            list.Add(entry);
        }
    }

    public bool TryGetEntry(string faunaDefinitionId, out FaunaPoolEntry entry)
    {
        return entriesByFaunaId.TryGetValue(faunaDefinitionId, out entry);
    }

    public bool TryGetEntriesByBiome(BiomeType biome, out List<FaunaPoolEntry> entries)
    {
        return entriesByBiome.TryGetValue(biome, out entries);
    }

    public bool HasAvailableCopy(string faunaDefinitionId)
    {
        return TryGetEntry(faunaDefinitionId, out var entry)
            && entry.RemainingCount > 0;
    }

    public bool ReserveCopy(string faunDefinitionId)
    {
        if (!TryGetEntry(faunDefinitionId, out var entry))
            return false;

        if (entry.RemainingCount <= 0)
            return false;

        entry.RemainingCount--;
        return true;
    }

    public bool ReturnCopy(string faunaDefinitionId)
    {
        if (!TryGetEntry(faunaDefinitionId, out var entry))
            return false;

        if (entry.RemainingCount >= entry.MaxPoolCount)
            return false;

        entry.RemainingCount++;
        return true;
    }

    public IEnumerable<FaunaPoolEntry> GetAllEntries()
    {
        return entriesByFaunaId.Values;
    }
}

public class FaunaPoolEntry
{
    public string FaunaDefinitionId;
    public int MaxPoolCount;
    public int RemainingCount;

    public FaunaPoolEntry(string unitDefinitionId, int maxPoolCount)
    {
        FaunaDefinitionId = unitDefinitionId;
        MaxPoolCount = maxPoolCount;
        RemainingCount = maxPoolCount;
    }
}