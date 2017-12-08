using System;
using System.Threading.Tasks;
using Orleans;

namespace GrainInterfaces
{
    public interface IPairingGrain : IGrainWithIntegerKey
    {
        Task AddGame(Guid gameId, string name);
        Task RemoveGame(Guid gameId);
        Task<PairingSummary[]> GetGames();
    }

    public class PairingSummary
    {
        public Guid GameId { get; set; }
        public string Name { get; set; }
    }
}