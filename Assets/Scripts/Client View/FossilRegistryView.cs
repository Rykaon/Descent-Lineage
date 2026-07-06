using TMPro;
using UnityEngine;
using UnityEngine.UI;

public sealed class FossilRegistryView : MonoBehaviour
{
    [SerializeField] private FossilRegistryTarget target;
    [SerializeField] private Button button;
    [SerializeField] private TMP_Text xpText;
    [SerializeField] private FossilRegistryPopUpView popup;

    private ClientGameMirror mirror;

    public void Initialize(ClientGameMirror mirror)
    {
        this.mirror = mirror;

        button.onClick.RemoveListener(OpenPopup);
        button.onClick.AddListener(OpenPopup);

        Refresh();
    }

    public void Refresh()
    {
        if (mirror == null || mirror.LocalPlayerId < 0)
        {
            return;
        }

        int playerId = ResolvePlayerId();

        if (playerId < 0 || playerId >= mirror.Players.Length)
        {
            return;
        }

        var fossil = mirror.Players[playerId].Fossil;
        xpText.text = $"Niveau suivant dans :\n{fossil.XpToNextLevel} XP";
    }

    private void OpenPopup()
    {
        int playerId = ResolvePlayerId();

        FossilRegistryViewMode mode = target == FossilRegistryTarget.LocalPlayer
            ? FossilRegistryViewMode.Local
            : FossilRegistryViewMode.Opponent;

        popup.Open(playerId, mode);
    }

    private int ResolvePlayerId()
    {
        return target == FossilRegistryTarget.LocalPlayer
            ? mirror.LocalPlayerId
            : mirror.LocalPlayerId == 0 ? 1 : 0;
    }
}