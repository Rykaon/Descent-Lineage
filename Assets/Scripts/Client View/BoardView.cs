using UnityEngine;
using System;
using System.Collections.Generic;

public class BoardView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameController gameController;
    [SerializeField] private Transform tileRoot;
    [SerializeField] private Transform unitRoot;
    [SerializeField] private BoardTileView tilePrefab;
    [SerializeField] private UnitView unitPrefab;

    [Header("Materials")]
    [SerializeField] private Material benchMaterial;
    [SerializeField] private Material defaultBoardMaterial;
    [SerializeField] private Material forestMaterial;
    [SerializeField] private Material swampMaterial;
    [SerializeField] private Material savannaMaterial;
    [SerializeField] private Material coastMaterial;
    [SerializeField] private Material mountainMaterial;

    private readonly Dictionary<BoardNode, BoardTileView> tiles = new();
    private readonly Dictionary<string, UnitView> unitViews = new();

    private void OnEnable()
    {
        gameController.OnBoardChanged += HandleBoardChanged;
    }

    private void OnDisable()
    {
        gameController.OnBoardChanged -= HandleBoardChanged;
    }

    private void Start()
    {
        PlayerState localPlayer = gameController.State.Players[0];
        Build(gameController.State.SharedBoard);
    }

    public UnitView GetUnitView(string unitInstanceId)
    {
        unitViews.TryGetValue(unitInstanceId, out UnitView view);
        return view;
    }

    private void HandleBoardChanged(PlayerState player)
    {
        if (player.PlayerId != 0)
        {
            return;
        }

        Refresh(gameController.State.SharedBoard);
    }

    public void Build(SharedBoardState board)
    {
        Clear();

        foreach (var pair in board.Tiles)
        {
            BoardTileState tile = pair.Value;

            BoardTileView go = Instantiate(tilePrefab, tileRoot);
            go.transform.position = board.NodeToWorld(tile.Node);

            Material mat = GetMaterial(tile);
            go.Initialize(tile.Node, mat);

            tiles[tile.Node] = go;
        }

        RefreshUnits(gameController.State.Players[0].Board);
    }

    public void Refresh(SharedBoardState board)
    {
        RefreshTiles(board);
        RefreshUnits(gameController.State.Players[0].Board);
        RefreshUnits(gameController.State.Players[1].Board);
    }

    private void RefreshTiles(SharedBoardState board)
    {
        foreach (var pair in board.Tiles)
        {
            BoardTileState tile = pair.Value;

            if (!tiles.TryGetValue(tile.Node, out var view))
                continue;

            view.transform.GetChild(0).GetComponent<Renderer>().material = GetMaterial(tile);
        }
    }

    private void RefreshUnits(PlayerBoardState board)
    {
        foreach (var unit in board.Units)
        {
            if (!unitViews.TryGetValue(unit.InstanceId, out UnitView unitView))
            {
                UnitView go = Instantiate(unitPrefab, unitRoot);
                unitViews[unit.InstanceId] = go;
                go.Bind(unit);
                go.SetPosition(gameController.State.SharedBoard.NodeToWorld(unit.Node));
            }
            else
            {
                unitView.SetPosition(gameController.State.SharedBoard.NodeToWorld(unit.Node));
            }
        }
    }

    private Material GetMaterial(BoardTileState tile)
    {
        if (tile.Location == BoardType.Bench)
            return benchMaterial;

        return tile.Biome switch
        {
            BiomeType.Forest => forestMaterial,
            BiomeType.Swamp => swampMaterial,
            BiomeType.Savanna => savannaMaterial,
            BiomeType.Coast => coastMaterial,
            BiomeType.Mountain => mountainMaterial,
            BiomeType.None => defaultBoardMaterial,
            _ => defaultBoardMaterial
        };
    }

    /*private void HandleTileClicked(BoardNode node)
    {
        gameController.TryPlaceSelectedUnitOnNode(node);
    }*/

    public void ClearUnit(UnitView unit)
    {
        unitViews.Remove(unit.UnitInstanceId);
        Destroy(unit.gameObject);
    }

    private void Clear()
    {
        foreach (Transform child in tileRoot)
        {
            Destroy(child.gameObject);
        }

        foreach (Transform child in unitRoot)
        {
            Destroy(child.gameObject);
        }

        tiles.Clear();
        unitViews.Clear();
    }
}