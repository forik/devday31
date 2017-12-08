using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using GrainInterfaces;
using Orleans;
using Orleans.Concurrency;
using Orleans.Providers;

namespace Grains
{
    /// <summary>
    ///     Orleans grain implementation class GameGrain
    /// </summary>
    [Reentrant]
    [StorageProvider(ProviderName = "Default")]
    public class GameGrain : Grain<GameStateStore>, IGameGrain
    {
        public Guid WinnerId { get; private set; } // set when game is over
        public Guid LoserId { get; private set; } // set when game is over
        
        public async Task<GameState> AddPlayerToGame(Guid player)
        {
            State.AddPlayer(player);
            await WriteStateAsync();

            // let user know if game is ready or not
            return State.GameState;
        }

        public async Task<GameState> MakeMove(GameMove move)
        {
            State.MakeMove(move);

            // check for a winning move
            var win = false;
            for (var i = 0; i < 3 && !win; i++)
                win = IsWinningLine(State.TheBoard[i, 0], State.TheBoard[i, 1], State.TheBoard[i, 2]);
            if (!win)
                for (var i = 0; i < 3 && !win; i++)
                    win = IsWinningLine(State.TheBoard[0, i], State.TheBoard[1, i], State.TheBoard[2, i]);
            if (!win)
                win = IsWinningLine(State.TheBoard[0, 0], State.TheBoard[1, 1], State.TheBoard[2, 2]);
            if (!win)
                win = IsWinningLine(State.TheBoard[0, 2], State.TheBoard[1, 1], State.TheBoard[2, 0]);

            // check for draw
            var draw = State.ListOfMoves.Count == 9;

            // handle end of game
            if (win || draw)
            {
                // game over
                State.GameState = GameState.Finished;
                if (win)
                {
                    WinnerId = State.CurrentPlayer;
                    LoserId = State.NextPlayer;
                }

                // collect tasks up, so we await both notifications at the same time
                var promises = new List<Task>();
                // inform this player of outcome
                var playerGrain = GrainFactory.GetGrain<IPlayerGrain>(State.CurrentPlayer);
                promises.Add(playerGrain.LeaveGame(this.GetPrimaryKey(), win ? GameOutcome.Win : GameOutcome.Draw));

                // inform other player of outcome
                playerGrain = GrainFactory.GetGrain<IPlayerGrain>(State.NextPlayer);
                promises.Add(playerGrain.LeaveGame(this.GetPrimaryKey(), win ? GameOutcome.Lose : GameOutcome.Draw));

                await Task.WhenAll(promises);

                return State.GameState;
            }

            State.AdvanceNextPlayer();
            await WriteStateAsync();

            return State.GameState;
        }

        public Task<GameState> GetState()
        {
            return Task.FromResult(State.GameState);
        }

        public Task<List<GameMove>> GetMoves()
        {
            return Task.FromResult(State.ListOfMoves);
        }

        public async Task<GameSummary> GetSummary(Guid player)
        {
            var promises = State.ListOfPlayers.Where(p => p != player)
                .Select(p => GrainFactory.GetGrain<IPlayerGrain>(p).GetUsername()).ToList();
            var userNames = await Task.WhenAll(promises);

            return new GameSummary
            {
                NumMoves = State.ListOfMoves.Count,
                State = State.GameState,
                YourMove = State.GameState == GameState.InPlay && player == State.CurrentPlayer,
                NumPlayers = State.ListOfPlayers.Count,
                GameId = this.GetPrimaryKey(),
                Usernames = userNames,
                Name = State.Name,
                GameStarter = State.ListOfPlayers.FirstOrDefault() == player
            };
        }

        public async Task SetName(string name)
        {
            State.Name = name;
            await WriteStateAsync();
        }

        // initialise 
        public override Task OnActivateAsync()
        {
            if (State == null)
            {
                State = new GameStateStore();
            }

            return base.OnActivateAsync();
        }

        private static bool IsWinningLine(int i, int j, int k)
        {
            if (i == 0 && j == 0 && k == 0) return true;
            if (i == 1 && j == 1 && k == 1) return true;
            return false;
        }
    }

    public class GameStateStore
    {
        public int IndexNextPlayerToMove { get; set; }
        public string Name { get; set; }
        public int[,] TheBoard { get; set; }

        // list of players in the current game
        // for simplicity, player 0 always plays an "O" and player 1 plays an "X"
        //  who starts a game is a random call once a game is started, and is set via indexNextPlayerToMove
        public List<Guid> ListOfPlayers { get; set; }

        public GameState GameState { get; set; }
        public Guid WinnerId { get; set; } // set when game is over
        public Guid LoserId { get; set; } // set when game is over

        // we record a game in terms of each of the moves, so we could reconstruct the sequence of play
        // during an active game, we also use a 2D array to represent the board, to make it
        //  easier to check for legal moves, wining lines, etc. 
        //  -1 represents an empty square, 0 & 1 the player's index 
        public List<GameMove> ListOfMoves { get; set; }

        public Guid CurrentPlayer => ListOfPlayers[IndexNextPlayerToMove];
        public Guid NextPlayer => ListOfPlayers[(IndexNextPlayerToMove + 1) % 2];

        public GameStateStore()
        {
            ListOfPlayers = new List<Guid>();
            ListOfMoves = new List<GameMove>();
            IndexNextPlayerToMove = -1; // safety default - is set when game begins to 0 or 1
            TheBoard = new int[3, 3] { { -1, -1, -1 }, { -1, -1, -1 }, { -1, -1, -1 } }; // -1 is empty

            GameState = GameState.AwaitingPlayers;
            WinnerId = Guid.Empty;
            LoserId = Guid.Empty;
        }

        public void AddPlayer(Guid player)
        {
            // check if its ok to join this game
            if (GameState == GameState.Finished) throw new ApplicationException("Can't join game once its over");
            if (GameState == GameState.InPlay) throw new ApplicationException("Can't join game once its in play");

            // add player
            ListOfPlayers.Add(player);

            // check if the game is ready to play
            if (GameState == GameState.AwaitingPlayers && ListOfPlayers.Count == 2)
            {
                // a new game is starting
                GameState = GameState.InPlay;
                IndexNextPlayerToMove = new Random().Next(0, 1); // random as to who has the first move
            }
        }

        public void AdvanceNextPlayer()
        {
            // if game hasnt ended, prepare for next players move
            IndexNextPlayerToMove = (IndexNextPlayerToMove + 1) % 2;
        }

        public void MakeMove(GameMove move)
        {
            // check if its a legal move to make
            if (GameState != GameState.InPlay)
                throw new ApplicationException("This game is not in play");

            if (ListOfPlayers.IndexOf(move.PlayerId) < 0)
                throw new ArgumentException("No such playerid for this game", "move");
            if (move.PlayerId != CurrentPlayer)
                throw new ArgumentException("The wrong player tried to make a move", "move");

            if (move.X < 0 || move.X > 2 || move.Y < 0 || move.Y > 2)
                throw new ArgumentException("Bad co-ordinates for a move", "move");
            if (TheBoard[move.X, move.Y] != -1)
                throw new ArgumentException("That square is not empty", "move");

            ListOfMoves.Add(move);
            TheBoard[move.X, move.Y] = IndexNextPlayerToMove;
        }
    }
}