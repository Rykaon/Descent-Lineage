using System;
using UnityEngine;

public class BoardTileView : MonoBehaviour
{
    public BoardNode Node { get; private set; }
    private Renderer TileRenderer;
    [SerializeField] private HexTileVisualizer Visualizer;

    public void Initialize(BoardNode node, Material material)
    {
        HideVisualizer();
        Node = node;

        TileRenderer = transform.GetChild(0).GetComponent<Renderer>();
        TileRenderer.material = material;
        Visualizer = transform.GetChild(1).GetComponent<HexTileVisualizer>();
    }

    public void ShowVisualizer(bool value)
    {
        Visualizer.Show(value);
    }

    public void HideVisualizer()
    {
        Visualizer.Hide();
    }
}
