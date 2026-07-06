using System.Collections.Generic;
using UnityEngine;

public sealed class NetworkSnapshotBuilder
{
    private readonly GameController gameController;
    private readonly IUnitDefinitionDatabase unitDatabase;
    private readonly IMutationDefinitionDatabase mutationDatabase;
    private readonly IFaunaDefinitionDatabase faunaDatabase;

    public NetworkSnapshotBuilder(GameController gameController)
    {
        this.gameController = gameController;
        unitDatabase = gameController.UnitDatabase;
        mutationDatabase = gameController.MutationDatabase;
        faunaDatabase = gameController.FaunaDatabase;
    }

    public BoardStateSnapshot BuildBoardStateSnapshot()
    {
        List<BoardUnitSnapshot> unitSnapshots = new();
        List<BoardTileSnapshot> tileSnapshots = new();

        foreach (var player in gameController.State.Players)
        {
            foreach (var unit in player.Board.Units)
            {
                unitSnapshots.Add(new BoardUnitSnapshot
                {
                    InstanceId = unit.InstanceId,
                    DefinitionId = unit.DefinitionId,
                    OwnerPlayerId = unit.OwnerPlayerId,
                    Node = unit.Node
                });
            }
        }

        foreach (var tile in gameController.State.SharedBoard.Tiles.Values)
        {
            tileSnapshots.Add(new BoardTileSnapshot
            {
                Node = tile.Node,
                OwnerPlayerId = tile.HomePlayerId,
                BoardType = tile.Location,
                BiomeType = tile.Biome
            });
        }

        return new BoardStateSnapshot
        {
            Units = unitSnapshots.ToArray(),
            Tiles = tileSnapshots.ToArray()
        };
    }

    public PlayerStateSnapshot[] BuildPlayerStateSnapshot()
    {
        List<PlayerStateSnapshot> snapshots = new();

        foreach (var player in gameController.State.Players)
        {
            snapshots.Add(new PlayerStateSnapshot
            {
                PlayerId = player.PlayerId,

                Life = player.Life,
                Amber = player.AmberCount,
                BiomeBudget = player.BiomeCount,
                BoardCapacity = player.Board.BoardCapacity,
                UnitsOnBoard = player.Board.UnitsOnBoard
            });
        }

        return snapshots.ToArray();
    }

    public ShopStateSnapshot BuildShopStateSnapshot()
    {
        List<ShopSlotSnapshot> slots = new();

        foreach (var player in gameController.State.Players)
        {
            for (int i = 0; i < player.Shop.Slots.Length; i++)
            {
                ShopSlot slot = player.Shop.Slots[i];

                string mutationId = "";

                if (slot != null && slot.MutationsId != null && slot.MutationsId.Count > 0)
                {
                    mutationId = slot.MutationsId[slot.MutationsId.Count - 1];
                }

                slots.Add(new ShopSlotSnapshot
                {
                    PlayerId = player.PlayerId,
                    SlotIndex = i,

                    UnitDefinitionId = slot == null ? "" : slot.UnitDefinitionId,
                    MutationId = mutationId,

                    Cost = slot == null ? 0 : slot.Cost,
                    IsEmpty = slot == null || string.IsNullOrEmpty(slot.UnitDefinitionId),
                    IsBrick = slot != null && slot.IsBrick,
                    IsMutationUpgrade = slot != null && slot.IsMutationUpgrade
                });
            }
        }

        return new ShopStateSnapshot
        {
            Slots = slots.ToArray()
        };
    }

    public FaunaShopStateSnapshot BuildFaunaShopStateSnapshot()
    {
        List<FaunaShopSlotSnapshot> slots = new();

        foreach (var player in gameController.State.Players)
        {
            for (int i = 0; i < player.FaunaShop.Slots.Length; i++)
            {
                FaunaShopSlot slot = player.FaunaShop.Slots[i];

                string mutationId = "";

                if (slot != null && slot.MutationId != null)
                {
                    mutationId = slot.MutationId;
                }

                slots.Add(new FaunaShopSlotSnapshot
                {
                    PlayerId = player.PlayerId,
                    SlotIndex = i,

                    FaunaDefinitionId = slot == null ? "" : slot.FaunaDefinitionId,
                    MutationId = mutationId,

                    FossileValue = slot == null ? 0 : slot.FossilValue,
                    Cost = slot == null ? 0 : slot.Cost,
                    IsBrick = slot != null && slot.IsBrick,
                });
            }
        }

        return new FaunaShopStateSnapshot
        {
            Slots = slots.ToArray()
        };
    }

    public FossilStateSnapshot[] BuildFossilStateSnapshot()
    {
        var players = gameController.State.Players;
        var snapshots = new FossilStateSnapshot[players.Length];

        for (int i = 0; i < players.Length; i++)
        {
            var player = players[i];

            snapshots[i] = BuildSingleFossilStateSnapshot(player.PlayerId, player.Fossil);
        }

        return snapshots;
    }

    private FossilStateSnapshot BuildSingleFossilStateSnapshot(int playerId, FossilState fossil)
    {
        int nextLevelXp = FossilSystem.GetNextLevelRequiredValue(fossil.Level);
        int xpToNextLevel = FossilSystem.GetXpToNextLevel(fossil.FossilValue, fossil.Level);

        var mutationSnapshots = new FossilMutationSnapshot[fossil.Mutations.Count];

        int index = 0;

        foreach (var pair in fossil.Mutations)
        {
            string mutationId = pair.Key;

            MutationDefinition mutationDefinition = mutationDatabase.GetMutation(mutationId);

            mutationSnapshots[index] = new FossilMutationSnapshot
            {
                MutationId = mutationId,
                DisplayName = mutationDefinition.DisplayName,
                Biome = GetPrimaryBiome(mutationDefinition),
            };

            index++;
        }

        return new FossilStateSnapshot
        {
            PlayerId = playerId,
            FossilLevel = fossil.Level,
            CurrentXp = fossil.FossilValue,
            NextLevelXp = nextLevelXp,
            XpToNextLevel = xpToNextLevel,
            Mutations = mutationSnapshots
        };
    }

    private BiomeType GetPrimaryBiome(MutationDefinition mutationDefinition)
    {
        if (mutationDefinition.EligibleBiomes == null || mutationDefinition.EligibleBiomes.Count == 0)
        {
            return BiomeType.None;
        }

        return mutationDefinition.EligibleBiomes[0];
    }

    public BattleInitSnapshot BuildBattleInitSnapshot()
    {
        List<BattleInitUnitSnapshot> units = new();

        for (int i = 0; i < gameController.Battle.BattleState.Units.Count; i++)
        {
            BattleUnitInstance unit = gameController.Battle.BattleState.Units[i];

            units.Add(new BattleInitUnitSnapshot
            {
                UnitIndex = i,

                BattleInstanceId = unit.BattleInstanceId,
                BoardInstanceId = unit.BoardInstanceId,
                DefinitionId = unit.DefinitionId,

                OwnerPlayerId = unit.OwnerPlayerId,

                CurrentHexQ = unit.CurrentHex.Q,
                CurrentHexR = unit.CurrentHex.R,

                CurrentHealth = unit.CurrentHealth,
                MaxHealth = unit.MaxHealth,

                AttackSpeed = unit.CurrentStats.AttackSpeed,
                MoveSpeed = unit.CurrentStats.MoveSpeed
            });
        }

        return new BattleInitSnapshot
        {
            Units = units.ToArray()
        };
    }

    public BattleFrameSnapshot BuildBattleFrameSnapshot()
    {
        List<BattleFrameUnitSnapshot> units = new();

        for (int i = 0; i < gameController.Battle.BattleState.Units.Count; i++)
        {
            BattleUnitInstance unit = gameController.Battle.BattleState.Units[i];

            int targetIndex = -1;

            if (!string.IsNullOrEmpty(unit.CurrentTargetBattleInstanceId))
            {
                targetIndex = gameController.Battle.BattleState.Units.FindIndex(u => u.BattleInstanceId == unit.CurrentTargetBattleInstanceId);
            }

            units.Add(new BattleFrameUnitSnapshot
            {
                UnitIndex = i,

                CurrentHexQ = unit.CurrentHex.Q,
                CurrentHexR = unit.CurrentHex.R,

                CurrentHealth = unit.CurrentHealth,
                IsDead = unit.IsDead,

                AttackSpeed = unit.CurrentStats.AttackSpeed,
                MoveSpeed = unit.CurrentStats.MoveSpeed,

                TargetUnitIndex = targetIndex
            });
        }

        return new BattleFrameSnapshot
        {
            Units = units.ToArray()
        };
    }

    public BattleEventsSnapshot BuildBattleEventsSnapshot()
    {
        List<BattleDamageEventSnapshot> damageEvents = new();

        foreach (BattleDamageEvent damageEvent in gameController.Battle.EventBuffer.DamageEvents)
        {
            damageEvents.Add(new BattleDamageEventSnapshot
            {
                SourceBattleInstanceId = string.IsNullOrEmpty(damageEvent.SourceBattleInstanceId)
                    ? ""
                    : damageEvent.SourceBattleInstanceId,

                TargetBattleInstanceId = string.IsNullOrEmpty(damageEvent.TargetBattleInstanceId)
                    ? ""
                    : damageEvent.TargetBattleInstanceId,

                Amount = damageEvent.Amount,
                Delivery = damageEvent.Delivery
            });
        }

        return new BattleEventsSnapshot
        {
            DamageEvents = damageEvents.ToArray()
        };
    }
}