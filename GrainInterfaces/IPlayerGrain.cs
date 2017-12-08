using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface IPlayerGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// get a list of all active games
        /// </summary>
        /// <returns></returns>
        Task<PairingSummary[]> GetAvailableGames();

        Task<List<GameSummary>> GetGameSummaries();

        /// <summary>
        /// create a new game and join it
        /// </summary>
        /// <returns></returns>
        Task<Guid> CreateGame();

        /// <summary>
        /// join an existing game
        /// </summary>
        Task<GameState> JoinGame(Guid gameId);

        Task LeaveGame(Guid gameId, GameOutcome outcome);

        Task SetUsername(string username);

        Task<string> GetUsername();
    }
}