using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface IGameGrain : IGrainWithGuidKey
    {
        /// <summary>
        /// add a player into a game
        /// </summary>
        Task<GameState> AddPlayerToGame(Guid player);
        Task<GameState> GetState();
        Task<List<GameMove>> GetMoves();
        Task<GameState> MakeMove(GameMove move);
        Task<GameSummary> GetSummary(Guid player);
        Task SetName(string name);
    }


    /// <summary>
    /// define the possible states a game can be in
    /// </summary>
    public enum GameState
    {
        AwaitingPlayers,
        InPlay,
        Finished
    }


    /// <summary>
    /// define game outcomes
    /// </summary>
    public enum GameOutcome
    {
        Win,
        Lose,
        Draw
    }


    public struct GameMove
    {
        public Guid PlayerId { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
    }

    public struct GameSummary
    {
        public GameState State { get; set; }
        public bool YourMove { get; set; }
        public int NumMoves { get; set; }
        public GameOutcome Outcome { get; set; }
        public int NumPlayers { get; set; }
        public Guid GameId { get; set; }
        public string[] Usernames { get; set; }
        public string Name { get; set; }
        public bool GameStarter { get; set; }
        public bool Waiting { get; set; }
    }
}