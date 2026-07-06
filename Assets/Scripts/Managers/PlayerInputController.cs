using UnityEngine;
using System.Collections.Generic;
using UnityEngine.EventSystems;

public class PlayerInputController : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private LayerMask tileMask;
    [SerializeField] private LayerMask unitMask;
    [SerializeField] private LayerMask biomeMask;

    [SerializeField] private BoardView boardView;
    [SerializeField] private BattleView battleView;
    [SerializeField] private SellDropZoneView sellDropZoneView;
    [SerializeField] private EventSystem eventSystem;

    private LocalPlayerContext context;
    private ClientGameMirror mirror;
    private IGameCommandSender commandSender;

    public bool isDraggingUnit;
    private UnitView draggedUnitView;
    private string draggedUnitInstanceId;

    public bool isDraggingBiome;
    private BoardBiomeRessourceView draggedBiomeView;

    private BoardTileView currentDestinationTile;

    public void Initialize(LocalPlayerContext context, IGameCommandSender commandSender, ClientGameMirror mirror)
    {
        this.context = context;
        this.commandSender = commandSender;
        this.mirror = mirror;

        boardView.Initialize(mirror);
        battleView.Initialize(mirror);
    }

    public void HandlePhaseChanged(GamePhase phase)
    {
        if (phase == GamePhase.PreBattle && isDraggingUnit)
        {
            draggedUnitView.SetDragging(false, true);

            draggedUnitView = null;
            draggedUnitInstanceId = null;
            isDraggingUnit = false;
        }
    }

    public void BuyShopSlot(int slotIndex)
    {
        GameCommandResult result = commandSender.SubmitCommand(new GameCommand
        {
            PlayerId = context.PlayerId,
            Type = GameCommandType.BuyShopUnit,
            ShopSlotIndex = slotIndex,
        });
    }

    public void BuyFaunaShopSlot(int slotIndex)
    {
        GameCommandResult result = commandSender.SubmitCommand(new GameCommand
        {
            PlayerId = context.PlayerId,
            Type = GameCommandType.BuyShopFauna,
            ShopSlotIndex = slotIndex,
        });
    }

    public void RefreshShop()
    {
        Debug.Log("[INPUT] RefreshShop pressed");

        GameCommandResult result = commandSender.SubmitCommand(new GameCommand
        {
            PlayerId = context.PlayerId,
            Type = GameCommandType.RerollShop,
        });
    }

    public void BeginDragUnit(UnitView unitView)
    {
        ClientPlayerMirror player = mirror.LocalPlayer;

        if (player == null)
        {
            return;
        }

        if (!player.Board.TryGetUnit(unitView.UnitInstanceId, out ClientBoardUnitMirror unit))
        {
            return;
        }

        if (mirror.Phase != GamePhase.Preparation)
        {
            if (!mirror.SharedBoard.TryGetTile(unit.Node, out ClientBoardTileMirror tile))
            {
                return;
            }

            if (tile.BoardType != BoardType.Bench)
            {
                return;
            }
        }

        draggedUnitView = unitView;
        draggedUnitInstanceId = unitView.UnitInstanceId;

        unitView.SetDragging(true, false);
        isDraggingUnit = true;

        sellDropZoneView.Show(unit.SellCost);
    }

    public void EndDragUnit()
    {
        if (draggedUnitView == null)
        {
            return;
        }

        if (TryGetSellDropZoneUnderMouse())
        {
            GameCommandResult result = commandSender.SubmitCommand(new GameCommand
            {
                Type = GameCommandType.SellUnit,
                UnitInstanceId = draggedUnitInstanceId,
            });

            ClearDraggedUnit(true);
            return;
        }

        if (TryGetTileUnderMouse(out BoardTileView tileView))
        {
            GameCommandResult result = commandSender.SubmitCommand(new GameCommand
            {
                Type = GameCommandType.DropBoardUnit,
                UnitInstanceId = draggedUnitInstanceId,
                ToNode = tileView.Node
            });

            ClearDraggedUnit(true);
            return;
        }

        ClearDraggedUnit(true);
    }

    private void ClearDraggedUnit(bool resetPosition)
    {
        if (draggedUnitView != null)
        {
            draggedUnitView.SetDragging(false, resetPosition);
        }

        if (currentDestinationTile != null)
        {
            currentDestinationTile.HideVisualizer();
        }

        currentDestinationTile = null;
        draggedUnitView = null;
        draggedUnitInstanceId = null;
        isDraggingUnit = false;

        sellDropZoneView.Hide();
    }

    private bool TryGetSellDropZoneUnderMouse()
    {
        PointerEventData pointerData = new PointerEventData(eventSystem)
        {
            position = Input.mousePosition
        };

        List<RaycastResult> results = new();
        eventSystem.RaycastAll(pointerData, results);

        foreach (RaycastResult result in results)
        {
            if (result.gameObject.GetComponentInParent<SellDropZoneView>() != null)
                return true;
        }

        return false;
    }

    public void BeginDragBiome(BoardBiomeView biome)
    {
        BoardBiomeRessourceView biomeRessource = biome.CreateRessourceBiome();

        isDraggingBiome = true;
        draggedBiomeView = biomeRessource;
        sellDropZoneView.Show(EconomySettings.BiomeToAmberConversion);
    }

    public void EndDragBiome()
    {
        if (draggedBiomeView == null)
        {
            return;
        }

        if (TryGetSellDropZoneUnderMouse())
        {
            GameCommandResult result = commandSender.SubmitCommand(new GameCommand
            {
                PlayerId = context.PlayerId,
                Type = GameCommandType.SellBiome,
            });

            ClearDraggedBiome();
            return;
        }

        if (TryGetTileUnderMouse(out BoardTileView tileView))
        {
            GameCommandResult result = commandSender.SubmitCommand(new GameCommand
            {
                PlayerId = context.PlayerId,
                Type = GameCommandType.DropBiomeTile,
                BiomeType = draggedBiomeView.biomeType,
                ToNode = tileView.Node
            });

            ClearDraggedBiome();
            return;
        }

        ClearDraggedBiome();
    }

    private void ClearDraggedBiome()
    {
        if (draggedBiomeView != null)
        {
            Destroy(draggedBiomeView.gameObject);
        }

        draggedBiomeView = null;
        isDraggingBiome = false;

        sellDropZoneView.Hide();
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        Plane plane = new Plane(Vector3.up, Vector3.zero);

        if (plane.Raycast(ray, out float distance))
            return ray.GetPoint(distance) + Vector3.up * 0.6f;

        return Vector3.zero;
    }

    private bool TryGetTileUnderMouse(out BoardTileView tileView)
    {
        tileView = null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, tileMask))
        {
            return false;
        }

        return hit.collider.transform.parent.TryGetComponent(out tileView);
    }

    private bool TryGetUnitUnderMouse(out UnitView unitView)
    {
        unitView = null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, unitMask))
        {
            return false;
        }

        return hit.collider.transform.parent.TryGetComponent(out unitView);
    }

    private bool TryGetBiomeUnderMouse(out BoardBiomeView biomeView)
    {
        biomeView = null;

        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);

        if (!Physics.Raycast(ray, out RaycastHit hit, 100f, biomeMask))
        {
            return false;
        }

        return hit.collider.transform.parent.TryGetComponent(out biomeView);
    }

    private bool TryDropUnitAtTile(UnitView unit, BoardTileView tile)
    {
        Debug.Log("TRY DROP UNIT ENTER");

        if (!mirror.Players[context.PlayerId].Board.TryGetUnit(unit.UnitInstanceId, out var clientUnit))
        {
            Debug.Log("PLAYER NOT FOUND IN MIRROR");
            return false;
        }

        if (clientUnit.OwnerPlayerId != context.PlayerId)
        {
            return false;
        }

        if (!mirror.SharedBoard.TryGetTile(clientUnit.Node, out var unitTile))
        {
            Debug.Log("UNIT TILE NOT FOUND IN MIRROR");
            return false;
        }

        if (!mirror.SharedBoard.TryGetTile(tile.Node, out var destinationTile))
        {
            Debug.Log("DESTINATION TILE NOT FOUND IN MIRROR");
            return false;
        }

        if (destinationTile.OwnerPlayerId != context.PlayerId)
        {
            return false;
        }

        if (mirror.Phase != GamePhase.Preparation)
        {
            if (destinationTile != null && destinationTile.BoardType != BoardType.Bench)
            {
                Debug.Log("JE SAIS PAS");
                return false;
            }
        }
        else
        {
            if (destinationTile == null)
            {
                Debug.Log("DESTINATION TILE NULL");
                return false;
            }

            if (unitTile.BoardType == BoardType.Bench)
            {
                if (destinationTile.BoardType == BoardType.Board)
                {
                    if (mirror.Players[context.PlayerId].UnitsOnBoard >= mirror.Players[context.PlayerId].BoardCapacity)
                    {
                        Debug.Log("TOO MUCH UNIT ON BOARD");
                        return false;
                    }
                    else
                    {
                        if (mirror.Players[context.PlayerId].Board.UnitIdByNode.ContainsKey(destinationTile.Node))
                        {
                            if (mirror.Players[context.PlayerId].Board.UnitIdByNode.TryGetValue(destinationTile.Node, out var other) && other != clientUnit.InstanceId)
                            {
                                Debug.Log("ALREADY UNIT ON NODE");
                                return false;
                            }
                        }
                    }
                }
                else
                {
                    if (mirror.Players[context.PlayerId].Board.UnitIdByNode.ContainsKey(destinationTile.Node))
                    {
                        if (mirror.Players[context.PlayerId].Board.UnitIdByNode.TryGetValue(destinationTile.Node, out var other) && other != clientUnit.InstanceId)
                        {
                            Debug.Log("ALREADY UNIT ON NODE");
                            return false;
                        }
                    }
                }
            }
            else
            {
                if (destinationTile.BoardType == BoardType.Board)
                {
                    if (mirror.Players[context.PlayerId].Board.UnitIdByNode.ContainsKey(destinationTile.Node))
                    {
                        if (mirror.Players[context.PlayerId].Board.UnitIdByNode.TryGetValue(destinationTile.Node, out var other) && other != clientUnit.InstanceId)
                        {
                            Debug.Log("ALREADY UNIT ON NODE");
                            return false;
                        }
                    }
                }
                else
                {
                    if (mirror.Players[context.PlayerId].Board.UnitIdByNode.ContainsKey(destinationTile.Node))
                    {
                        if (mirror.Players[context.PlayerId].Board.UnitIdByNode.TryGetValue(destinationTile.Node, out var other) && other != clientUnit.InstanceId)
                        {
                            Debug.Log("ALREADY UNIT ON NODE");
                            return false;
                        }
                    }
                }
            }
        }

        Debug.Log("CAN DROP UNIT");

        return true;
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            RefreshShop();
        }

        if (Input.GetMouseButtonDown(0))
        {

            if (TryGetUnitUnderMouse(out UnitView unitView))
            {
                BeginDragUnit(unitView);
            }
            else if (TryGetBiomeUnderMouse(out BoardBiomeView biomeView))
            {
                BeginDragBiome(biomeView);
            }
        }
        else if (Input.GetMouseButtonUp(0))
        {
            if (isDraggingUnit)
            {
                EndDragUnit();
            }
            else if (isDraggingBiome)
            {
                EndDragBiome();
            }
        }

        if (draggedUnitView != null)
        {
            draggedUnitView.transform.position = GetMouseWorldPosition();

            if (!TryGetTileUnderMouse(out var tileView))
            {
                if (currentDestinationTile != null)
                {
                    currentDestinationTile.HideVisualizer();
                    currentDestinationTile = null;
                }
            }
            else
            {
                if (currentDestinationTile != null)
                {
                    if (currentDestinationTile != tileView)
                    {
                        currentDestinationTile.HideVisualizer();
                        currentDestinationTile = tileView;

                        if (TryDropUnitAtTile(draggedUnitView, tileView))
                        {
                            currentDestinationTile.ShowVisualizer(true);
                        }
                        else
                        {
                            currentDestinationTile.ShowVisualizer(false);
                            Debug.Log("False");
                        }
                    }
                }
                else 
                {
                    currentDestinationTile = tileView;

                    if (TryDropUnitAtTile(draggedUnitView, tileView))
                    {
                        currentDestinationTile.ShowVisualizer(true);
                    }
                    else
                    {
                        currentDestinationTile.ShowVisualizer(false);
                    }
                }
            }

            if (sellDropZoneView != null)
            {
                sellDropZoneView.SetHighlighted(TryGetSellDropZoneUnderMouse());
            }
        }
        else if (draggedBiomeView != null)
        {
            draggedBiomeView.transform.position = GetMouseWorldPosition();

            if (sellDropZoneView != null)
            {
                sellDropZoneView.SetHighlighted(TryGetSellDropZoneUnderMouse());
            }
        }
    }
}

public class LocalPlayerContext
{
    public int PlayerId { get; private set; }

    public LocalPlayerContext(int playerId)
    {
        PlayerId = playerId;
    }
}
