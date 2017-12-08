using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Web.Controllers
{
    public class GameController : Controller
    {
        private readonly IClusterClient _client;

        public GameController(IClusterClient client)
        {
            _client = client;
        }

        private Guid GetGuid()
        {
            if (Request.Cookies["playerId"] != null)
            {
                return Guid.Parse(this.Request.Cookies["playerId"]);
            }
            var guid = Guid.NewGuid();
            Response.Cookies.Append("playerId", guid.ToString());
            return guid;
        }

        public async Task<ActionResult> Index()
        {
            var guid = GetGuid();
            var player = _client.GetGrain<IPlayerGrain>(guid);
            var gamesTask = player.GetGameSummaries();
            var availableTask = player.GetAvailableGames();
            await Task.WhenAll(gamesTask, availableTask);

            var currentGames = gamesTask.Result;
            currentGames.ForEach(x => x.Waiting = x.State == GameState.AwaitingPlayers);

            return Ok(new object[] { currentGames, availableTask.Result });
        }

        [HttpPost]
        public async Task<ActionResult> CreateGame()
        {
            var guid = GetGuid();
            var player = _client.GetGrain<IPlayerGrain>(guid);
            var gameIdTask = await player.CreateGame();
            return Ok(new { GameId = gameIdTask });
        }

        [HttpPost]
        public async Task<ActionResult> Join(Guid id)
        {
            var guid = GetGuid();
            var player = _client.GetGrain<IPlayerGrain>(guid);
            var state = await player.JoinGame(id);
            return Ok(new { GameState = state });
        }

        public async Task<ActionResult> GetMoves(Guid id)
        {
            var guid = GetGuid();
            var game = _client.GetGrain<IGameGrain>(id);
            var moves = await game.GetMoves();
            var summary = await game.GetSummary(guid);
            return Ok(new { moves = moves, summary = summary });
        }

        [HttpPost]
        public async Task<ActionResult> MakeMove(Guid id, int x, int y)
        {
            var guid = GetGuid();
            var game = _client.GetGrain<IGameGrain>(id);
            var move = new GameMove { PlayerId = guid, X = x, Y = y };
            var state = await game.MakeMove(move);
            return Ok(state);
        }

        public async Task<ActionResult> QueryGame(Guid id)
        {
            var game = _client.GetGrain<IGameGrain>(id);
            var state = await game.GetState();
            return Ok(state);

        }

        [HttpPost]
        public async Task<ActionResult> SetUser(string name)
        {
            var guid = GetGuid();
            var player = _client.GetGrain<IPlayerGrain>(guid);
            await player.SetUsername(name);

            return Ok();
        }
    }
}