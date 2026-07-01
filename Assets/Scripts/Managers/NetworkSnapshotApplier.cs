using System.Collections.Generic;
using UnityEngine;

public sealed class NetworkSnapshotApplier : MonoBehaviour
{
    [SerializeField] private GameController gameController;
    [SerializeField] private BoardView boardView;
    [SerializeField] private ShopView shopView;
    [SerializeField] private FaunaShopView faunaShopView;
    [SerializeField] private BattleView battleView;

    public ClientGameMirror Mirror { get; private set; } = new();

    public BattleClientState BattleReplicationState { get; private set; }

    public void ApplyPhaseState(GamePhaseSnapshot snapshot)
    {
        Mirror.Phase = snapshot.Phase;

        gameController.SetClientPhase(snapshot.Phase);

        if (Mirror.Phase == GamePhase.PostBattle)
        {
            ApplyBattleEnded();
        }

        Debug.Log($"[CLIENT MIRROR] Phase={snapshot.Phase}");
    }

    public void ApplyBoardState(BoardStateSnapshot snapshot)
    {
        ApplyBoardMirror(snapshot);
        ApplyBoardUnits(snapshot);
        ApplyBoardTiles(snapshot);

        if (!boardView.IsBuilt)
        {
            boardView.Build(Mirror);
        }
        else
        {
            boardView.Refresh(Mirror);
        }
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

            player.Amber = snapshot.Amber;
            player.BiomeCount = snapshot.BiomeBudget;
            player.BoardCapacity = snapshot.BoardCapacity;
            player.UnitsOnBoard = snapshot.UnitsOnBoard;
        }

        Debug.Log($"[CLIENT MIRROR] Player states applied. count={snapshots.Length}");
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
        shopView.Refresh(Mirror.LocalPlayer.Shop);
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
        faunaShopView.Refresh(Mirror.LocalPlayer.FaunaShop);
    }

    private void ApplyBoardUnits(BoardStateSnapshot snapshot)
    {
        foreach (var player in gameController.State.Players)
        {
            player.Board.Units.Clear();
            player.Board.UnitByInstanceId.Clear();
            player.Board.UnitByDefinitionId.Clear();
            player.Board.UnitIdByNode.Clear();
            player.Board.UnitsOnBoard = 0;
        }

        foreach (BoardUnitSnapshot unitSnapshot in snapshot.Units)
        {
            PlayerState player = gameController.State.GetPlayer(unitSnapshot.OwnerPlayerId);

            BoardUnitInstance unit = new(
                unitSnapshot.DefinitionId.ToString(),
                unitSnapshot.OwnerPlayerId,
                unitSnapshot.Node);

            unit.InstanceId = unitSnapshot.InstanceId.ToString();

            unit.MutationIds = new();

            gameController.State.SharedBoard.TryGetTile(unit.Node, out BoardTileState tile);
            player.Board.RegisterUnit(unit, tile);
        }
    }

    private void ApplyBoardTiles(BoardStateSnapshot snapshot)
    {
        foreach (BoardTileSnapshot tileSnapshot in snapshot.Tiles)
        {
            if (!gameController.State.SharedBoard.TryGetTile(tileSnapshot.Node, out BoardTileState tile))
            {
                continue;
            }

            tile.HomePlayerId = tileSnapshot.OwnerPlayerId;
            tile.Location = tileSnapshot.BoardType;
            tile.Biome = tileSnapshot.BiomeType;
        }
    }

    public void ApplyBattleInit(BattleInitSnapshot snapshot)
    {
        BattleReplicationState = new BattleClientState();

        foreach (BattleInitUnitSnapshot unitSnapshot in snapshot.Units)
        {
            BattleHexCoord hex = new(
                unitSnapshot.CurrentHexQ,
                unitSnapshot.CurrentHexR);

            Vector2 position = gameController.Battle.HexGrid.HexToWorld(hex);

            BattleReplicationState.AddUnit(new BattleClientUnit
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

                AttackSpeed = unitSnapshot.AttackSpeed,
                MoveSpeed = unitSnapshot.MoveSpeed,

                IsDead = false,
                CurrentTargetBattleInstanceId = null
            });
        }

        battleView.Bind(BattleReplicationState);
    }

    public void ApplyBattleFrame(BattleFrameSnapshot snapshot)
    {
        battleView.ApplyBattleFrame(snapshot, gameController.Battle.HexGrid);
    }

    public void ApplyBattleEvents(BattleEventsSnapshot snapshot)
    {
        battleView.ApplyBattleEvents(snapshot);
    }

    public void ApplyBattleEnded()
    {
        battleView.HandleBattleEnded();
    }
}