using System;
using System.Collections;
using UnityEngine;

public class GameController : MonoBehaviour
{
    public event Action<GameState> OnGameStateChanged;
    public event Action<PlayerState> OnShopChanged;
    public event Action<PlayerState> OnBoardChanged;
    public event Action<GamePhase> OnPhaseChanged;
    public event Action<int> OnPreparationTicked;
    public event Action<BattleState> OnBattleStarted;
    public event Action<BattleState> OnBattleTicked;
    public event Action OnBattleEnded;

    [SerializeField] private GameSessionConfig sessionConfig;
    [SerializeField] private NetworkGameBridge networkGameBridge;
    [SerializeField] private PlayerInputController localInput;
    [SerializeField] private UnitDefinitionDatabaseAsset unitDatabaseAsset;
    [SerializeField] private MutationDefinitionDatabaseAsset mutationDatabaseAsset;
    [SerializeField] private FaunaDefinitionDatabaseAsset faunaDefinitionDatabaseAsset;

    private GameState state;

    private ShopSystem shopSystem;
    private FaunaShopSystem faunaShopSystem;
    private BoardSystem boardSystem;
    private FossilSystem fossilSystem;
    private EconomySystem economySystem;
    private BattleSystem battleSystem;

    private IUnitDefinitionDatabase unitDatabase;
    private IMutationDefinitionDatabase mutationDatabase;
    private IFaunaDefinitionDatabase faunaDatabase;

    public IUnitDefinitionDatabase UnitDatabase => unitDatabase;
    public IMutationDefinitionDatabase MutationDatabase => mutationDatabase;
    public IFaunaDefinitionDatabase FaunaDatabase => faunaDatabase;

    private PlayerState winner;
    private PlayerState loser;

    public EconomySystem Economy => economySystem;
    public BattleSystem Battle => battleSystem;

    public GameState State => state;

    private Coroutine preparationRoutine;
    private Coroutine battleRoutine;

    private void Start()
    {
        state = new GameState();
        state.Initialize(this);

        InitializeGame();
        InitializeEnemy();
    }

    private IGameCommandSender CreateCommandSender(NetworkGameRole role)
    {
        switch (role)
        {
            case NetworkGameRole.Local:
                return new LocalGameCommandSender(this);

            case NetworkGameRole.Client:
                return new NetworkGameCommandSender(networkGameBridge);

            case NetworkGameRole.DedicatedServer:
                return null;

            default:
                return new LocalGameCommandSender(this);
        }
    }

    private void InitializeGame()
    {
        unitDatabase = unitDatabaseAsset.Build();
        mutationDatabase = mutationDatabaseAsset.Build();
        faunaDatabase = faunaDefinitionDatabaseAsset.Build();

        economySystem = new EconomySystem();
        economySystem.Initialize();

        shopSystem = new ShopSystem();
        shopSystem.Initialize(unitDatabase, mutationDatabase, state.SharedBoard);

        faunaShopSystem = new FaunaShopSystem();
        faunaShopSystem.Initialize(faunaDatabase, mutationDatabase, state.SharedBoard);

        fossilSystem = new FossilSystem();
        fossilSystem.Initialize();

        boardSystem = new BoardSystem();
        boardSystem.Initialize(state.SharedBoard);

        battleSystem = new BattleSystem();
        battleSystem.Initialize(unitDatabase, mutationDatabase, state.SharedBoard);
    }

    private void InitializeEnemy()
    {
        PlayerState enemyState = state.GetPlayer(1);

        state.SharedBoard.TryGetTile(new BoardNode(3, 6), out BoardTileState tile1);
        state.SharedBoard.TryGetTile(new BoardNode(4, 5), out BoardTileState tile2);
        state.SharedBoard.TryGetTile(new BoardNode(5, 5), out BoardTileState tile3);
        state.SharedBoard.TryGetTile(new BoardNode(6, 5), out BoardTileState tile4);
        state.SharedBoard.TryGetTile(new BoardNode(7, 6), out BoardTileState tile5);
        state.SharedBoard.TryGetTile(new BoardNode(2, 5), out BoardTileState tile6);
        state.SharedBoard.TryGetTile(new BoardNode(8, 5), out BoardTileState tile7);
        state.SharedBoard.TryGetTile(new BoardNode(1, 6), out BoardTileState tile8);

        state.SharedBoard.TryGetTile(new BoardNode(3, 5), out BoardTileState tile9);
        state.SharedBoard.TryGetTile(new BoardNode(4, 5), out BoardTileState tile10);

        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Lystrosaurus", 1, new BoardNode(3, 6)), tile1);
        enemyState.Board.RegisterUnit(new BoardUnitInstance("Dunkleosteus", 1, new BoardNode(4, 5)), tile2);
        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Anomalocaris", 1, new BoardNode(5, 5)), tile3);
        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Tiktaalik", 1, new BoardNode(6, 5)), tile4);
        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Thylacosmilus", 1, new BoardNode(7, 6)), tile5);
        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Hallucigenia", 1, new BoardNode(2, 5)), tile6);
        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Quetzalcoatlus", 1, new BoardNode(8, 5)), tile7);
        //enemyState.Board.RegisterUnit(new BoardUnitInstance("Paraceratherium", 1, new BoardNode(1, 6)), tile8);

    }

    public void StartGame()
    {
        EnterPhase(GamePhase.Setup);
    }

    public void SetClientPhase(GamePhase phase)
    {
        state.Phase = phase;
        OnPhaseChanged?.Invoke(phase);
    }

    private void EnterPhase(GamePhase phase)
    {
        state.Phase = phase;
        OnPhaseChanged.Invoke(phase);

        switch (phase)
        {
            case GamePhase.Setup:
                RunSetupPhase();
                break;

            case GamePhase.Preparation:
                preparationRoutine = StartCoroutine(RunPreparationPhase());
                break;

            case GamePhase.PreBattle:
                RunPreBattlePhase();
                break;

            case GamePhase.Battle:
                battleRoutine = StartCoroutine(RunBattlePhase());
                break;

            case GamePhase.PostBattle:
                RunPostBattlePhase();
                break;

            case GamePhase.End:
                RunEndPhase();
                break;
        }
        
        OnGameStateChanged?.Invoke(state);
    }

    private void RunSetupPhase()
    {
        foreach (PlayerState player in state.Players)
        {
            if (state.RoundIndex == 0)
            {
                economySystem.ApplyInitIncome(player);

                if (player.PlayerId == 0)
                {
                    Debug.Log("Player has " + player.AmberCount + " Ambers and " + player.BiomeCount + " BiomeTiles.");
                }
            }
            else
            {
                economySystem.ApplyIncome(player, state.SharedBoard);

                if (player.PlayerId == 0)
                {
                    Debug.Log("Player has " + player.AmberCount + " Ambers and " + player.BiomeCount + " BiomeTiles.");
                }
            }

            fossilSystem.ApplyLevel(player);
        }

        shopSystem.FillAllShopsRoundRobin(state, new System.Random());
        faunaShopSystem.FillAllShopsRoundRobin(state, new System.Random());

        foreach (PlayerState player in state.Players)
        {
            OnShopChanged?.Invoke(player);
        }

        EnterPhase(GamePhase.Preparation);
    }

    private IEnumerator RunPreparationPhase()
    {
        int duration = 20;
        float elapsed = duration;
        int elapsedRounded = duration;
        OnPreparationTicked.Invoke(elapsedRounded);

        while (elapsed > 0)
        {
            elapsed -= Time.deltaTime;

            if (Mathf.RoundToInt(elapsed) != elapsedRounded)
            {
                elapsedRounded = Mathf.RoundToInt(elapsed);
                OnPreparationTicked.Invoke(elapsedRounded);
            }
            
            yield return null;
        }

        EnterPhase(GamePhase.PreBattle);
    }

    private void RunPreBattlePhase()
    {
        battleSystem.StartServerBattle(state);

        OnBattleStarted?.Invoke(battleSystem.BattleState);
        EnterPhase(GamePhase.Battle);
    }

    private IEnumerator RunBattlePhase()
    {
        const float BattleFixedDelta = 1f / 30f;
        const int MaxBattleTicksPerFrame = 2;
        int battleTickCount = 0;
        float battleTickTimer = 0;

        float accumulator = 0f;

        yield return null;

        while (!battleSystem.BattleState.IsBattleFinished)
        {
            accumulator += Time.deltaTime;

            int tickCount = 0;

            while (accumulator >= BattleFixedDelta &&
                   tickCount < MaxBattleTicksPerFrame)
            {
                battleSystem.Tick(BattleFixedDelta);
                OnBattleTicked?.Invoke(battleSystem.BattleState);
                battleTickCount++;
                accumulator -= BattleFixedDelta;
                tickCount++;

                if (battleSystem.TryGetWinner(battleSystem.BattleState, out int winnerPlayerId))
                {
                    state.RoundWinner = state.GetPlayer(winnerPlayerId);
                    battleSystem.BattleState.WinningPlayerId = winnerPlayerId;
                    battleSystem.BattleState.IsBattleFinished = true;
                    break;
                }
            }

            battleTickTimer += Time.deltaTime;

            if (battleTickTimer >= 1f)
            {
                //Debug.Log($"Battle ticks/sec = {battleTickCount}");
                battleTickCount = 0;
                battleTickTimer = 0f;
            }

            if (tickCount >= MaxBattleTicksPerFrame)
            {
                accumulator = Mathf.Min(accumulator, BattleFixedDelta);
            }

            yield return null;
        }

        EnterPhase(GamePhase.PostBattle);
    }

    private void RunPostBattlePhase()
    {
        PlayerState RoundLoser = state.GetOtherPlayer(state.RoundWinner);
        RoundLoser.Life -= 10;

        RoundLoser.CalculateStreak(false);
        Debug.Log("Player has " + RoundLoser.Streak.Value + " loose streak");
        Debug.Log("Player has " + RoundLoser.AmberCount + " Ambers.");
        state.GetOtherPlayer(RoundLoser).CalculateStreak(true);
        state.GetOtherPlayer(RoundLoser).AddAmber(1);

        if (state.HasLoser(out PlayerState loser))
        {
            this.loser = loser;
            winner = state.GetOtherPlayer(loser);
            EnterPhase(GamePhase.End);
        }

        OnBattleEnded?.Invoke();
        state.RoundIndex++;
        EnterPhase(GamePhase.Setup);
    }

    private void RunEndPhase()
    {
        Debug.Log(loser.PlayerId + " a perdu !");
    }

    public GameCommandResult ApplyCommand(GameCommand command)
    {
        bool success = command.Type switch
        {
            GameCommandType.BuyShopUnit =>
                TryBuyShopSlot(command.PlayerId, command.ShopSlotIndex),

            GameCommandType.BuyShopFauna =>
                TryBuyFaunaShopSlot(command.PlayerId, command.ShopSlotIndex),

            GameCommandType.RerollShop =>
                TryRefreshShop(command.PlayerId),

            GameCommandType.DragBoardUnit =>
                TryDragUnit(command.PlayerId, command.UnitInstanceId.ToString()),

            GameCommandType.DropBoardUnit =>
                TryDropUnit(command.PlayerId, command.UnitInstanceId.ToString(), command.ToNode),

            GameCommandType.SellUnit =>
                TrySellUnit(command.PlayerId, command.UnitInstanceId.ToString()),

            GameCommandType.DropBiomeTile =>
                TryDropBiome(command.PlayerId, command.ToNode, command.BiomeType),

            GameCommandType.SellBiome =>
                TrySellBiome(command.PlayerId),

            _ => false
        };

        Debug.Log("[COMMAND SUCCES] Command=" + command.Type + " Success=" + success);

        return success ? GameCommandResult.Ok() : GameCommandResult.Fail(command.Type + " failed");
    }

    public bool TryBuyShopSlot(int playerId, int slotIndex)
    {
        if (state.Phase != GamePhase.Preparation && state.Phase != GamePhase.Battle)
        {
            return false;
        }

        PlayerState player = state.GetPlayer(playerId);

        bool success = shopSystem.TryBuySlot(player, slotIndex, state.Phase);

        if (!success)
        {
            return false;
        }

        shopSystem.UpdateShopSlotsAfterPurchase(player.Shop.Slots[slotIndex], player.Shop);
        player.Shop.Slots[slotIndex] = null;

        OnShopChanged?.Invoke(player);
        OnBoardChanged?.Invoke(player);
        OnGameStateChanged?.Invoke(state);
        return true;
    }

    public bool TryBuyFaunaShopSlot(int playerId, int slotIndex)
    {
        if (state.Phase != GamePhase.Preparation && state.Phase != GamePhase.Battle)
        {
            return false;
        }

        PlayerState player = state.GetPlayer(playerId);

        bool success = faunaShopSystem.TryBuySlot(player, slotIndex, state.Phase);

        if (!success)
        {
            return false;
        }

        player.FaunaShop.Slots[slotIndex] = null;
        fossilSystem.ApplyLevel(player);

        OnShopChanged?.Invoke(player);
        OnGameStateChanged?.Invoke(state);
        return true;
    }

    public bool TryRefreshShop(int playerId)
    {
        if (state.Phase != GamePhase.Preparation && state.Phase != GamePhase.Battle)
        {
            return false;
        }

        PlayerState player = state.GetPlayer(playerId);

        if (player.AmberCount < player.Shop.RefreshCost)
        {
            Debug.Log("Not enough Amber to refresh Shop.");
            return false;
        }

        shopSystem.RefreshPlayerShop(player, new System.Random());
        faunaShopSystem.RefreshPlayerShop(player, new System.Random());

        OnShopChanged?.Invoke(player);
        OnGameStateChanged?.Invoke(state);

        return true;
    }

    public bool TryDragUnit(int playerId, string unitInstanceId)
    {
        PlayerState player = state.GetPlayer(playerId);

        if (!player.Board.UnitByInstanceId.TryGetValue(unitInstanceId, out var unit))
        {
            return false;
        }

        return boardSystem.TryDragUnit(player, unit, state.Phase);
    }

    public bool TryDropUnit(int playerId, string unitInstanceId, BoardNode destination)
    {
        PlayerState player = state.GetPlayer(playerId);

        if (!player.Board.UnitByInstanceId.TryGetValue(unitInstanceId, out var unit))
        {
            return false;
        }

        if (!boardSystem.TryDropUnit(player, unit, destination, state.Phase))
        {
            return false;
        }

        boardSystem.DropUnit(player, unit, destination);

        OnBoardChanged?.Invoke(player);
        return true;
    }

    public bool TryDropBiome(int playerId, BoardNode destination, BiomeType biome)
    {
        PlayerState player = state.GetPlayer(playerId);

        if (!boardSystem.TryDropBiome(player, destination, biome, state.Phase))
        {
            return false;
        }

        boardSystem.DropBiome(player, destination, biome);

        OnBoardChanged?.Invoke(player);
        return true;
    }

    public bool TrySellUnit(int playerId, string unitInstanceId)
    {
        PlayerState player = state.GetPlayer(playerId);

        if (!player.Board.TryGetUnitByInstanceId(unitInstanceId, out var unit))
        {
            return false;
        }

        if (!state.SharedBoard.TryGetTile(unit.Node, out var tile))
        {
            return false;
        }

        if (tile.Location == BoardType.Board && (state.Phase == GamePhase.PreBattle || state.Phase == GamePhase.Battle || state.Phase == GamePhase.PostBattle))
        {
            return false;
        }

        player.Board.UnregisterUnit(unit, tile);
        player.AddAmber(EconomySystem.GetUnitSellCostFromMutationCount(unit.MutationIds.Count));
        Debug.Log("Player has " + player.AmberCount + " Ambers.");

        return true;
    }

    public bool TrySellBiome(int playerId)
    {
        PlayerState player = state.GetPlayer(playerId);

        if (player.BiomeCount == 0)
        {
            return false;
        }

        player.AddAmber(EconomySettings.BiomeToAmberConversion);
        player.BiomeCount--;

        Debug.Log("Player has " + player.AmberCount + " Ambers.");
        return true;
    }
}