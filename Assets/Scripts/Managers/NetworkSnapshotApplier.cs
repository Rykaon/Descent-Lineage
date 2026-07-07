using System.Collections.Generic;
using UnityEngine;

public sealed class NetworkSnapshotApplier
{
    private readonly ClientGameContext context;

    public ClientGameMirror Mirror => context.Mirror;

    public BattleClientState BattleReplicationState { get; private set; }

    public NetworkSnapshotApplier(ClientGameContext context)
    {
        this.context = context;
    }

    public void ApplyPhaseState(GamePhaseSnapshot snapshot)
    {
        Mirror.Phase = snapshot.Phase;

        if (Mirror.Phase == GamePhase.PostBattle)
        {
            context.NotifyBattleEnded();
        }

        context.NotifyPhaseChanged();
        Debug.Log($"[CLIENT MIRROR] Phase={snapshot.Phase}");
    }

    public void ApplyTimer(int remainingTime)
    {
        Mirror.PreparationRemainingTime = remainingTime;
        context.NotifyPreparationTicked();
    }

    public void ApplyBoardState(BoardStateSnapshot snapshot)
    {
        ApplyBoardMirror(snapshot);
        context.NotifyBoardChanged();
    }

    public void ApplyCladeStates(CladeStateSnapshot[] snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            ClientPlayerMirror player = context.Mirror.Players[snapshot.PlayerId];

            player.Clades.Entries.Clear();

            foreach (var clade in snapshot.Clades)
            {
                player.Clades.Entries.Add(new ClientCladeProgressMirror
                {
                    CladeId = clade.CladeId.ToString(),
                    Count = clade.Count,
                    NextThreshold = clade.NextThreshold,
                    IsActive = clade.IsActive
                });
            }
        }

        context.NotifyCladeChanged();
    }

    private void ApplyBoardMirror(BoardStateSnapshot snapshot)
    {
        foreach (ClientPlayerMirror player in Mirror.Players)
        {
            player.Board.UnitsById.Clear();
            player.Board.UnitIdByNode.Clear();
        }

        Mirror.SharedBoard.Tiles.Clear();

        foreach (BoardTileSnapshot tileSnapshot in snapshot.Tiles)
        {
            Mirror.SharedBoard.Tiles[tileSnapshot.Node] = new ClientBoardTileMirror
            {
                Node = tileSnapshot.Node,
                OwnerPlayerId = tileSnapshot.OwnerPlayerId,
                BoardType = tileSnapshot.BoardType,
                BiomeType = tileSnapshot.BiomeType
            };
        }

        foreach (BoardUnitSnapshot unitSnapshot in snapshot.Units)
        {
            ClientPlayerMirror player = Mirror.GetPlayer(unitSnapshot.OwnerPlayerId);

            ClientBoardUnitMirror unit = new()
            {
                InstanceId = unitSnapshot.InstanceId.ToString(),
                DefinitionId = unitSnapshot.DefinitionId.ToString(),
                OwnerPlayerId = unitSnapshot.OwnerPlayerId,
                Node = unitSnapshot.Node,
                MutationCount = unitSnapshot.MutationCount,
            };

            player.Board.UnitsById[unit.InstanceId] = unit;
            player.Board.UnitIdByNode[unit.Node] = unit.InstanceId;
        }
    }

    public void ApplyPlayerStates(PlayerStateSnapshot[] snapshots)
    {
        foreach (var snapshot in snapshots)
        {
            ClientPlayerMirror player = Mirror.GetPlayer(snapshot.PlayerId);

            player.Life = snapshot.Life;
            player.Amber = snapshot.Amber;
            player.BiomeCount = snapshot.BiomeBudget;
            player.BoardCapacity = snapshot.BoardCapacity;
            player.UnitsOnBoard = snapshot.UnitsOnBoard;
        }

        Debug.Log($"[CLIENT MIRROR] Player states applied. count={snapshots.Length}");
        context.NotifyPlayerChanged();
    }

    private void ApplyShopMirror(ShopStateSnapshot snapshot)
    {
        foreach (ShopSlotSnapshot slotSnapshot in snapshot.Slots)
        {
            ClientPlayerMirror player = Mirror.GetPlayer(slotSnapshot.PlayerId);

            ShopSlot slot = null;

            if (!slotSnapshot.IsEmpty)
            {
                if (slotSnapshot.IsBrick)
                {
                    slot = ShopSlot.CreateBrickReservedCreature(
                        slotSnapshot.UnitDefinitionId.ToString());
                }
                else if (slotSnapshot.IsMutationUpgrade)
                {
                    List<string> mutations = new();

                    string mutationId = slotSnapshot.MutationId.ToString();

                    if (!string.IsNullOrEmpty(mutationId))
                    {
                        mutations.Add(mutationId);
                    }

                    slot = ShopSlot.CreateMutationUpgrade(
                        slotSnapshot.UnitDefinitionId.ToString(),
                        mutations,
                        slotSnapshot.Cost);
                }
                else
                {
                    slot = ShopSlot.CreateNewCreature(
                        slotSnapshot.UnitDefinitionId.ToString(),
                        slotSnapshot.Cost);
                }
            }

            player.Shop.Slots[slotSnapshot.SlotIndex] = slot;
        }
    }

    public void ApplyShopState(ShopStateSnapshot snapshot)
    {
        ApplyShopMirror(snapshot);
        context.NotifyShopChanged();
    }

    private void ApplyFaunaShopMirror(FaunaShopStateSnapshot snapshot)
    {
        foreach (FaunaShopSlotSnapshot slotSnapshot in snapshot.Slots)
        {
            ClientPlayerMirror player = Mirror.GetPlayer(slotSnapshot.PlayerId);

            FaunaShopSlot slot;

            if (slotSnapshot.IsBrick)
            {
                slot = FaunaShopSlot.CreateBrick();
            }
            else
            {
                slot = FaunaShopSlot.CreateShopSlot(
                    slotSnapshot.FaunaDefinitionId.ToString(),
                    slotSnapshot.MutationId.ToString(),
                    slotSnapshot.FossileValue,
                    slotSnapshot.Cost);
            }

            player.FaunaShop.Slots[slotSnapshot.SlotIndex] = slot;
        }
    }

    public void ApplyFaunaShopState(FaunaShopStateSnapshot snapshot)
    {
        ApplyFaunaShopMirror(snapshot);
        context.NotifyFaunaShopChanged();
    }

    public void ApplyFossilStates(FossilStateSnapshot[] snapshots)
    {
        if (snapshots == null)
        {
            return;
        }

        foreach (var snapshot in snapshots)
        {
            if (snapshot.PlayerId < 0 || snapshot.PlayerId >= Mirror.Players.Length)
            {
                continue;
            }

            ApplyFossilMirror(Mirror.Players[snapshot.PlayerId].Fossil, snapshot);
        }

        context.NotifyFossilChanged();
    }

    private void ApplyFossilMirror(ClientFossilMirror fossilMirror, FossilStateSnapshot snapshot)
    {
        fossilMirror.PlayerId = snapshot.PlayerId;
        fossilMirror.FossilLevel = snapshot.FossilLevel;
        fossilMirror.CurrentXp = snapshot.CurrentXp;
        fossilMirror.NextLevelXp = snapshot.NextLevelXp;
        fossilMirror.XpToNextLevel = snapshot.XpToNextLevel;

        fossilMirror.Mutations.Clear();

        if (snapshot.Mutations == null)
        {
            return;
        }

        foreach (var mutationSnapshot in snapshot.Mutations)
        {
            fossilMirror.Mutations.Add(new ClientFossilMutationMirror
            {
                MutationId = mutationSnapshot.MutationId.ToString(),
                DisplayName = mutationSnapshot.DisplayName.ToString(),
                Biome = mutationSnapshot.Biome,
                Rank = mutationSnapshot.Rank
            });
        }

        Debug.Log($"[CLIENT FOSSIL] player={fossilMirror.PlayerId} level={fossilMirror.FossilLevel} xp={fossilMirror.CurrentXp} toNext={fossilMirror.XpToNextLevel} mutations={fossilMirror.Mutations.Count}");
    }

    public void ApplyBattleInit(BattleInitSnapshot snapshot)
    {
        BattleClientState battleState = new BattleClientState();

        foreach (BattleInitUnitSnapshot unitSnapshot in snapshot.Units)
        {
            BattleHexCoord hex = new(
                unitSnapshot.CurrentHexQ,
                unitSnapshot.CurrentHexR);

            Vector2 position = BoardGeometry.HexToWorld2D(hex);

            battleState.AddUnit(new BattleClientUnit
            {
                BattleInstanceId = unitSnapshot.BattleInstanceId.ToString(),
                BoardInstanceId = unitSnapshot.BoardInstanceId.ToString(),
                DefinitionId = unitSnapshot.DefinitionId.ToString(),

                OwnerPlayerId = unitSnapshot.OwnerPlayerId,

                CurrentHex = hex,
                Position = position,
                LastPosition = position,

                CurrentHealth = unitSnapshot.CurrentHealth,
                MaxHealth = unitSnapshot.MaxHealth,
                CurrentMana = unitSnapshot.CurrentMana,
                MaxMana = unitSnapshot.MaxMana,

                AttackSpeed = unitSnapshot.AttackSpeed,
                MoveSpeed = unitSnapshot.MoveSpeed,

                IsDead = false,
                CurrentTargetBattleInstanceId = null
            });
        }

        context.SetBattleState(battleState);
    }

    public void ApplyBattleFrame(BattleFrameSnapshot snapshot)
    {
        BattleClientState battleState = context.BattleState;

        if (battleState == null)
        {
            return;
        }

        foreach (BattleFrameUnitSnapshot unitSnapshot in snapshot.Units)
        {
            if (unitSnapshot.UnitIndex < 0 ||
                unitSnapshot.UnitIndex >= battleState.Units.Count)
            {
                continue;
            }

            BattleClientUnit unit = battleState.Units[unitSnapshot.UnitIndex];

            BattleHexCoord hex = new(unitSnapshot.CurrentHexQ, unitSnapshot.CurrentHexR);

            unit.CurrentHex = hex;
            unit.LastPosition = unit.Position;
            unit.Position = BoardGeometry.HexToWorld2D(hex);

            unit.CurrentHealth = unitSnapshot.CurrentHealth;
            unit.MaxHealth = unitSnapshot.MaxHealth;
            unit.CurrentMana = unitSnapshot.CurrentMana;
            unit.MaxMana = unitSnapshot.MaxMana;

            unit.IsDead = unitSnapshot.IsDead;

            unit.AttackSpeed = unitSnapshot.AttackSpeed;
            unit.MoveSpeed = unitSnapshot.MoveSpeed;

            int targetIndex = unitSnapshot.TargetUnitIndex;

            unit.CurrentTargetBattleInstanceId = targetIndex >= 0 && targetIndex < battleState.Units.Count ? battleState.Units[targetIndex].BattleInstanceId : null;
        }

        context.NotifyBattleFrameChanged();
    }

    public void ApplyBattleEvents(BattleEventsSnapshot snapshot)
    {
        context.NotifyBattleEventsReceived(snapshot);
    }
}