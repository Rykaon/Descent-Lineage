using UnityEngine;
using System;
using System.Collections.Generic;

public class BoardView : MonoBehaviour
{
    [Header("Refs")]
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

    private ClientGameMirror mirror;
    private readonly Dictionary<BoardNode, BoardTileView> tiles = new();
    private readonly Dictionary<string, UnitView> unitViews = new();
    public bool IsBuilt => tiles.Count > 0;

    public void Initialize(ClientGameMirror mirror)
    {
        this.mirror = mirror;
    }

    public UnitView GetUnitView(string unitInstanceId)
    {
        unitViews.TryGetValue(unitInstanceId, out UnitView view);
        return view;
    }

    public void Build(ClientGameMirror mirror)
    {
        Clear();

        foreach (var pair in mirror.SharedBoard.Tiles)
        {
            ClientBoardTileMirror tile = pair.Value;

            BoardTileView go = Instantiate(tilePrefab, tileRoot);
            go.transform.position = BoardGeometry.NodeToWorld(tile.Node);

            Material mat = GetMaterial(tile);
            go.Initialize(tile.Node, mat);

            tiles[tile.Node] = go;
        }

        Refresh(mirror);
    }

    public void Refresh(ClientGameMirror mirror)
    {
        RefreshTiles(mirror.SharedBoard);
        RemoveMissingUnitViews(mirror);
        RefreshUnits(mirror.Players[0].Board);
        RefreshUnits(mirror.Players[1].Board);
    }

    private void RemoveMissingUnitViews(ClientGameMirror mirror)
    {
        List<string> idsToRemove = new();

        foreach (var pair in unitViews)
        {
            string unitInstanceId = pair.Key;

            bool exists =
                mirror.Players[0].Board.UnitsById.ContainsKey(unitInstanceId) ||
                mirror.Players[1].Board.UnitsById.ContainsKey(unitInstanceId);

            if (!exists)
            {
                idsToRemove.Add(unitInstanceId);
            }
        }

        foreach (string id in idsToRemove)
        {
            UnitView view = unitViews[id];
            unitViews.Remove(id);
            Destroy(view.gameObject);
        }
    }

    private void RefreshTiles(ClientSharedBoardMirror board)
    {
        foreach (var pair in board.Tiles)
        {
            ClientBoardTileMirror tile = pair.Value;

            if (!tiles.TryGetValue(tile.Node, out var view))
            {
                continue;
            }

            view.transform.GetChild(0).GetComponent<Renderer>().material = GetMaterial(tile);
        }
    }

    private void RefreshUnits(ClientBoardMirror board)
    {
        foreach (var pair in board.UnitsById)
        {
            ClientBoardUnitMirror unit = pair.Value;

            if (!unitViews.TryGetValue(unit.InstanceId, out UnitView unitView))
            {
                UnitView go = Instantiate(unitPrefab, unitRoot);
                unitViews[unit.InstanceId] = go;

                go.Bind(unit.InstanceId);
                go.SetPosition(BoardGeometry.NodeToWorld(unit.Node));
            }
            else
            {
                unitView.SetPosition(BoardGeometry.NodeToWorld(unit.Node));
            }
        }
    }

    private Material GetMaterial(ClientBoardTileMirror tile)
    {
        if (tile.BoardType == BoardType.Bench)
        {
            return benchMaterial;
        }

        return tile.BiomeType switch
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