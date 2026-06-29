using System;
using UnityEngine;

public class BoardTileView : MonoBehaviour
{
    public BoardNode Node { get; private set; }
    private Renderer TileRenderer;

    public void Initialize(BoardNode node, Material material)
    {
        Node = node;

        TileRenderer = transform.GetChild(0).GetComponent<Renderer>();
        TileRenderer.material = material;
    }

    public void Bind(BoardTileState tile)
    {
        Node = tile.Node;
    }
}
