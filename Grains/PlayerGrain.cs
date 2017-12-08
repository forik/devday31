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
    ///     Orleans grain implementation class PlayerGrain
    /// </summary>
    [Reentrant]
    [StorageProvider(ProviderName = "Default")]
    public class PlayerGrain : Grain<PlayerState>, IPlayerGrain
    {
        public async Task<PairingSummary[]> GetAvailableGames()
        {
            var grain = GrainFactory.GetGrain<IPairingGrain>(0);
            return (await grain.GetGames()).Where(x => !State.ListOfActiveGames.Contains(x.GameId)).ToArray();
        }

        /// <inheritdoc />
        public async Task<Guid> CreateGame()
        {
            State.GamesStarted += 1;

            var gameId = Guid.NewGuid();
            var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId); // create new game

            // add ourselves to the game
            var playerId = this.GetPrimaryKey(); // our player id
            await gameGrain.AddPlayerToGame(playerId);

            State.ListOfActiveGames.Add(gameId);
            await WriteStateAsync();

            var name = State.Username + "'s " + AddOrdinalSuffix(State.GamesStarted.ToString()) + " game";
            await gameGrain.SetName(name);

            var pairingGrain = GrainFactory.GetGrain<IPairingGrain>(0);
            await pairingGrain.AddGame(gameId, name);

            return gameId;
        }


        /// <inheritdoc />
        /// <summary>
        ///     join a game that is awaiting players
        /// </summary>
        public async Task<GameState> JoinGame(Guid gameId)
        {
            var gameGrain = GrainFactory.GetGrain<IGameGrain>(gameId);

            var state = await gameGrain.AddPlayerToGame(this.GetPrimaryKey());
            State.ListOfActiveGames.Add(gameId);

            await WriteStateAsync();

            var pairingGrain = GrainFactory.GetGrain<IPairingGrain>(0);
            await pairingGrain.RemoveGame(gameId);

            return state;
        }


        // leave game when it is over
        public async Task LeaveGame(Guid gameId, GameOutcome outcome)
        {
            // manage game list
            State.ListOfActiveGames.Remove(gameId);
            State.ListOfPastGames.Add(gameId);

            // manage running total
            switch (outcome)
            {
                case GameOutcome.Win:
                    State.Wins++;
                    break;
                case GameOutcome.Lose:
                    State.Loses++;
                    break;
            }

            await WriteStateAsync();
        }

        public async Task<List<GameSummary>> GetGameSummaries()
        {
            var tasks = State.ListOfActiveGames
                .Select(gameId => GrainFactory.GetGrain<IGameGrain>(gameId))
                .Select(game => game.GetSummary(this.GetPrimaryKey())).ToList();

            var summaries = await Task.WhenAll(tasks);
            return summaries.ToList();
        }

        public async Task SetUsername(string name)
        {
            State.Username = name;
            await WriteStateAsync();
        }

        public Task<string> GetUsername()
        {
            return Task.FromResult(State.Username);
        }

        public override Task OnActivateAsync()
        {
            if (State == null)
            {
                State = new PlayerState();
            }

            return base.OnActivateAsync();
        }

        private static string AddOrdinalSuffix(string number)
        {
            var n = int.Parse(number);
            var nMod100 = n % 100;

            if (nMod100 >= 11 && nMod100 <= 13)
                return string.Concat(number, "th");

            switch (n % 10)
            {
                case 1:
                    return string.Concat(number, "st");
                case 2:
                    return string.Concat(number, "nd");
                case 3:
                    return string.Concat(number, "rd");
                default:
                    return string.Concat(number, "th");
            }
        }
    }

    public class PlayerState
    {
        public int GamesStarted { get; set; }
        public int Loses { get; set; }
        public string Username { get; set; }
        public int Wins { get; set; }
        public List<Guid> ListOfActiveGames { get; set; }
        public List<Guid> ListOfPastGames { get; set; }

        public PlayerState()
        {
            ListOfActiveGames = new List<Guid>();
            ListOfPastGames = new List<Guid>();

            GamesStarted = 0;
        }
    }
}