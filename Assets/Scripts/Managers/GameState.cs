using UnityEngine;

public class GameState
{
    public int RoundIndex;
    public GamePhase Phase;
    public PlayerState[] Players;
    private GameController Controller;

    public SharedBoardState SharedBoard;

    public PlayerState RoundWinner;

    public void Initialize(GameController controller)
    {
        Controller = controller;
        RoundIndex = 0;
        Phase = GamePhase.Initialization;

        Players = new[]
        {
            new PlayerState { PlayerId = 0 },
            new PlayerState { PlayerId = 1 }
        };

        foreach (PlayerState p in Players)
        {
            p.Initialize();
        }

        Controller.OnPhaseChanged += HandlePhaseChanged;

        SharedBoard = new SharedBoardState();
        SharedBoard.Initialize();
    }

    public PlayerState GetPlayer(int playerId)
    {
        foreach (PlayerState p in Players)
        {
            if (p.PlayerId == playerId)
            {
                return p;
            }
        }

        return null;
    }

    public PlayerState GetOtherPlayer(PlayerState player)
    {
        if (player == Players[0])
        {
            return Players[1];
        }
        else
        {
            return Players[0];
        }
    }

    private void HandlePhaseChanged(GamePhase phase)
    {
        Phase = phase;
    }

    public bool HasLoser(out PlayerState loser)
    {
        loser = null;

        foreach(PlayerState p in Players)
        {
            if (p.Life == 0)
            {
                loser = p;
                return true;
            }
        }

        return false;
    }
}
