#if UNITY_EDITOR
using UnityEditor;
#endif

using UnityEngine;

public class BattleHexGridDebugDraw : MonoBehaviour
{
    [SerializeField] private GameController gameController;

    [Header("Draw")]
    [SerializeField] private bool drawBoardCenters = true;
    [SerializeField] private bool drawOneFootprint = true;
    [SerializeField] private bool drawAllMicroHexes = false;

    [SerializeField] private Vector2Int debugBoardNode = new(4, 4);

    [SerializeField] private Color boardCenterColor = Color.green;
    [SerializeField] private Color footprintColor = Color.cyan;
    [SerializeField] private Color microGridColor = Color.white;

    private BattleHexGrid grid;

    private void OnDrawGizmos()
    {
        if (gameController == null ||
            gameController.State == null ||
            gameController.State.SharedBoard == null)
            return;

        grid = new BattleHexGrid(gameController.State.SharedBoard);

        if (drawAllMicroHexes)
            DrawAllMicroHexes();

        if (drawBoardCenters)
            DrawBoardCenters();

        if (drawOneFootprint)
            DrawOneFootprint();
    }

    private void DrawBoardCenters()
    {
        foreach (var pair in gameController.State.SharedBoard.Tiles)
        {
            BoardTileState tile = pair.Value;

            BattleHexCoord centerHex =
                grid.BoardNodeToBattleCenter(tile.Node);

            Vector2 p = grid.HexToWorld(centerHex);
            Vector3 center = new(p.x, 0f, p.y);

            Gizmos.color = boardCenterColor;
            Gizmos.DrawSphere(center, 0.08f);

#if UNITY_EDITOR
            Handles.Label(center + Vector3.up * 0.1f, $"{tile.Node.X},{tile.Node.Y}");
#endif
        }
    }

    private void DrawOneFootprint()
    {
        BoardNode node = new BoardNode(debugBoardNode.x, debugBoardNode.y);

        BattleHexCoord centerHex =
            grid.BoardNodeToBattleCenter(node);

        foreach (BattleHexCoord hex in grid.GetUnitFootprint(centerHex))
        {
            Vector2 p = grid.HexToWorld(hex);
            Vector3 center = new(p.x, 0.05f, p.y);

            Gizmos.color = footprintColor;
            DrawHex(center, grid.HexSize * 0.45f);
        }
    }

    private void DrawAllMicroHexes()
    {
        Gizmos.color = microGridColor;

        for (int q = 0; q < grid.Width; q++)
        {
            for (int r = 0; r < grid.Height; r++)
            {
                BattleHexCoord hex = new(q, r);
                Vector2 p = grid.HexToWorld(hex);
                DrawHex(new Vector3(p.x, 0f, p.y), grid.HexSize * 0.35f);
            }
        }
    }

    private void DrawHex(Vector3 center, float radius)
    {
        Vector3[] corners = new Vector3[6];

        for (int i = 0; i < 6; i++)
        {
            float angleDeg = 60f * i - 30f;
            float angleRad = angleDeg * Mathf.Deg2Rad;

            corners[i] = center + new Vector3(
                radius * Mathf.Cos(angleRad),
                0f,
                radius * Mathf.Sin(angleRad));
        }

        for (int i = 0; i < 6; i++)
            Gizmos.DrawLine(corners[i], corners[(i + 1) % 6]);
    }
}