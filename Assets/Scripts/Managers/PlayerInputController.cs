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
    [SerializeField] private SellDropZoneView sellDropZoneView;
    [SerializeField] private EventSystem eventSystem;

    private LocalPlayerContext context;
    private GameController gameController;

    public bool isDraggingUnit;
    private UnitView draggedUnitView;
    private string draggedUnitInstanceId;

    public bool isDraggingBiome;
    private BoardBiomeRessourceView draggedBiomeView;

    public void Initialize(LocalPlayerContext context, GameController gameController)
    {
        this.context = context;
        this.gameController = gameController;

        gameController.OnPhaseChanged += HandlePhaseChanged;
    }

    private void HandlePhaseChanged(GamePhase phase)
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
        gameController.TryBuyShopSlot(context.PlayerId, slotIndex);
    }

    public void BuyFaunaShopSlot(int slotIndex)
    {
        gameController.TryBuyFaunaShopSlot(context.PlayerId, slotIndex);
    }

    public void RefreshShop()
    {
        gameController.TryRefreshShop(context.PlayerId);
    }

    public void BeginDragUnit(UnitView unitView)
    {
        if (!gameController.TryDragUnit(context.PlayerId, unitView.UnitInstanceId))
        {
            return;
        }

        draggedUnitView = unitView;
        draggedUnitInstanceId = unitView.UnitInstanceId;
        unitView.SetDragging(true, false);

        isDraggingUnit = true;

        gameController.State.GetPlayer(context.PlayerId).Board.TryGetUnitByInstanceId(unitView.UnitInstanceId, out BoardUnitInstance unit);

        sellDropZoneView.Show(gameController.Economy.GetUnitSellCost(unit));
    }

    public void EndDragUnit()
    {
        if (draggedUnitView == null)
        {
            return;
        }

        if (TryGetSellDropZoneUnderMouse())
        {
            if (gameController.TrySellUnit(context.PlayerId, draggedUnitInstanceId))
            {
                boardView.ClearUnit(draggedUnitView);
            }

            ClearDraggedUnit(false);
            return;
        }

        if (TryGetTileUnderMouse(out BoardTileView tileView))
        {
            bool success = gameController.TryDropUnit(context.PlayerId, draggedUnitInstanceId, tileView.Node);

            ClearDraggedUnit(!success);
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
        sellDropZoneView.Show(gameController.Economy.Settings.BiomeToAmberConversion);
    }

    public void EndDragBiome()
    {
        if (draggedBiomeView == null)
        {
            return;
        }

        if (TryGetSellDropZoneUnderMouse())
        {
            if (gameController.TrySellBiome(context.PlayerId))
            {
                Destroy(draggedBiomeView.gameObject);
                draggedBiomeView = null;
                isDraggingBiome = false;

                sellDropZoneView.Hide();
                return;
            }

            Destroy(draggedBiomeView.gameObject);
            draggedBiomeView = null;
            isDraggingBiome = false;

            sellDropZoneView.Hide();
        }

        if (TryGetTileUnderMouse(out BoardTileView tileView))
        {
            gameController.TryDropBiome(context.PlayerId, tileView.Node, draggedBiomeView.biomeType);
        }

        Destroy(draggedBiomeView.gameObject);
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

        return hit.collider.TryGetComponent(out biomeView);
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
