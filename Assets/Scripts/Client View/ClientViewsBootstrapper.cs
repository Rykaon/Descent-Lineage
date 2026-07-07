using UnityEngine;
using System.Collections;

public sealed class ClientViewsBootstrapper : MonoBehaviour
{
    [SerializeField] private ClientGameContext context;

    [Header("General HUD")]
    [SerializeField] private CameraController cameraController;
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

    [Header("Clades UI")]
    [SerializeField] private CladePanelView cladePanelView;

    private bool isInitialized = false;

    private void Start()
    {
        if (context.HasInitialState)
        {
            HandleInitialStateReady();
        }
    }

    private void OnEnable()
    {
        if (context == null)
        {
            return;
        }

        context.InitialStateReady += HandleInitialStateReady;

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

        context.CladeChanged += RefreshClades;
    }

    private void OnDestroy()
    {
        if (context != null)
        {
            context.InitialStateReady -= HandleInitialStateReady;

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

            context.CladeChanged -= RefreshClades;
        }
    }

    private void HandleInitialStateReady()
    {
        cameraController.Initialize(context.Mirror);
        BuildOrRefreshBoard();
        RefreshResourcesCount();
        RefreshHealthBars();
        RefreshShop();
        RefreshFaunaShop();
        RefreshFossilUI();
        RefreshClades();

        localFossilPopup.Initialize(context.Mirror, context.UnitDatabase);
        opponentFossilPopup.Initialize(context.Mirror, context.UnitDatabase);

        localFossilButton.Initialize(context.Mirror);
        opponentFossilButton.Initialize(context.Mirror);

        isInitialized = true;

        RefreshFossilUI();
    }

    private void RefreshTimer()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        timerView.RefreshTimer(context.Mirror.PreparationRemainingTime);
    }

    private void BuildOrRefreshBoard()
    {
        if (!context.HasInitialState)
        {
            return;
        }

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
        if (!context.HasInitialState)
        {
            return;
        }

        battleView.Bind(context.BattleState);
    }

    private void HandleBattleFrameChanged()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        battleView.RefreshBattleFrame();
    }

    private void HandleBattleEventsReceived(BattleEventsSnapshot snapshot)
    {
        if (!context.HasInitialState)
        {
            return;
        }

        battleView.ApplyBattleEvents(snapshot);
    }

    private void HandleBattleEnded()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        battleView.HandleBattleEnded();
    }

    private void RefreshResourcesCount()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        shopView.RefreshResourcesCount(context.Mirror.LocalPlayer.Amber, context.Mirror.LocalPlayer.BiomeCount);
    }

    private void RefreshHealthBars()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        healthBarsView.SetValues(context.Mirror.LocalPlayer.Life, 100, context.Mirror.OpponentPlayer.Life, 100);
    }

    private void RefreshShop()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        shopView.Refresh(context.Mirror.LocalPlayer.Shop);
    }

    private void RefreshFaunaShop()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        faunaShopView.Refresh(context.Mirror.LocalPlayer.FaunaShop);
    }

    private void RefreshFossilUI()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        localFossilButton.Refresh();
        opponentFossilButton.Refresh();

        localFossilPopup.RefreshIfOpen();
        opponentFossilPopup.RefreshIfOpen();
    }

    private void RefreshClades()
    {
        if (!context.HasInitialState)
        {
            return;
        }

        cladePanelView.Refresh(context.Mirror.LocalPlayer.Clades);
    }
}