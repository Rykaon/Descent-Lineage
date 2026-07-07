using UnityEngine;

public class CameraController : MonoBehaviour
{
    [SerializeField] private Transform topSide;
    [SerializeField] private Transform bottomSide;

    private ClientGameMirror mirror;

    public void Initialize(ClientGameMirror mirror)
    {
        this.mirror = mirror;

        if (mirror.LocalPlayerId == 0)
        {
            PlaceOnBottomSide();
        }
        else
        {
            PlaceOnTopSide();
        }
    }

    private void PlaceOnBottomSide()
    {
        transform.position = bottomSide.position;
        transform.rotation = bottomSide.rotation;
    }

    private void PlaceOnTopSide()
    {
        transform.position = topSide.position;
        transform.rotation = topSide.rotation;
    }
}