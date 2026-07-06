using UnityEngine;

public sealed class ClientViewsBootstrapper : MonoBehaviour
{
    [SerializeField] private ClientGameContext context;
    
    [Header("General HUD")]
    [SerializeField] private TimerView timerView;
    [SerializeField] private PlayerHealthBarsView healthBarsView;

    [Header("Board & Battle")]
    [SerializeField] private BoardView boardView;
    [SerializeField] private BattleView battleView;

    [Header("Shops UI")]
    [SerializeField] private ShopView shopView;
    [SerializeField] private FaunaShopView faunaShopView;

    [Header("Fossil UI")]
    [SerializeField] private FossilRegistryPopUpView localFossilPopup;
    [SerializeField] private FossilRegistryPopUpView opponentFossilPopup;
    [SerializeField] private FossilRegistryView localFossilButton;
    [SerializeField] private FossilRegistryView opponentFossilButton;

    private bool isInitialized = false;

    private void Start()
    {
        localFossilPopup.Initialize(context.Mirror, context.UnitDatabase);
        opponentFossilPopup.Initialize(context.Mirror, context.UnitDatabase);

        localFossilButton.Initialize(context.Mirror);
        opponentFossilButton.Initialize(context.Mirror);

        isInitialized = true;

        RefreshFossilUI();
    }

    private void OnEnable()
    {
        if (context == null)
        {
            Debug.LogError("[VIEWS] Missing ClientGameContext reference.");
            return;
        }

        context.PreparationTicked += RefreshTimer;

        context.BattleInitChanged += HandleBattleInitChanged;
        context.BattleFrameChanged += HandleBattleFrameChanged;
        context.BattleEventsReceived += HandleBattleEventsReceived;
        context.BattleEnded += HandleBattleEnded;

        context.BoardChanged += BuildOrRefreshBoard;

        context.PlayerChanged += RefreshResourcesCount;
        context.PlayerChanged += RefreshHealthBars;

        context.ShopChanged += RefreshShop;
        context.FaunaShopChanged += RefreshFaunaShop;

        context.FossilChanged += RefreshFossilUI;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.PreparationTicked -= RefreshTimer;

            context.BattleInitChanged -= HandleBattleInitChanged;
            context.BattleFrameChanged -= HandleBattleFrameChanged;
            context.BattleEventsReceived -= HandleBattleEventsReceived;
            context.BattleEnded -= HandleBattleEnded;

            context.BoardChanged -= BuildOrRefreshBoard;

            context.PlayerChanged -= RefreshResourcesCount;
            context.PlayerChanged -= RefreshHealthBars;

            context.ShopChanged -= RefreshShop;
            context.FaunaShopChanged -= RefreshFaunaShop;

            context.FossilChanged -= RefreshFossilUI;
        }
    }

    private void RefreshTimer()
    {
        timerView.RefreshTimer(context.Mirror.PreparationRemainingTime);
    }

    private void BuildOrRefreshBoard()
    {
        if (!boardView.IsBuilt)
        {
            boardView.Build(context.Mirror);
        }
        else
        {
            boardView.Refresh(context.Mirror);
        }
    }

    private void HandleBattleInitChanged()
    {
        battleView.Bind(context.BattleState);
    }

    private void HandleBattleFrameChanged()
    {
        battleView.RefreshBattleFrame();
    }

    private void HandleBattleEventsReceived(BattleEventsSnapshot snapshot)
    {
        battleView.ApplyBattleEvents(snapshot);
    }

    private void HandleBattleEnded()
    {
        battleView.HandleBattleEnded();
    }

    private void RefreshResourcesCount()
    {
        shopView.RefreshResourcesCount(context.Mirror.LocalPlayer.Amber, context.Mirror.LocalPlayer.BiomeCount);
    }

    private void RefreshHealthBars()
    {
        healthBarsView.SetValues(context.Mirror.LocalPlayer.Life, 100, context.Mirror.OpponentPlayer.Life, 100);
    }

    private void RefreshShop()
    {
        shopView.Refresh(context.Mirror.LocalPlayer.Shop);
    }

    private void RefreshFaunaShop()
    {
        faunaShopView.Refresh(context.Mirror.LocalPlayer.FaunaShop);
    }

    private void RefreshFossilUI()
    {
        localFossilButton.Refresh();
        opponentFossilButton.Refresh();

        localFossilPopup.RefreshIfOpen();
        opponentFossilPopup.RefreshIfOpen();
    }
}