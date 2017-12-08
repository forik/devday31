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
    public class PairingGrain : Grain<PairingState>, IPairingGrain
    {
        public async Task AddGame(Guid gameId, string name)
        {
            State.Cache[gameId] = name;
            await WriteStateAsync();
        }

        public async Task RemoveGame(Guid gameId)
        {
            State.Cache.Remove(gameId);
            await WriteStateAsync();
        }

        public Task<PairingSummary[]> GetGames()
        {
            return Task.FromResult(State.Cache.Select(x => new PairingSummary {GameId = x.Key, Name = x.Value}).ToArray());
        }

        public override Task OnActivateAsync()
        {
            if (State.Cache == null)
            {
                State.Cache = new Dictionary<Guid, string>();
            }

            return base.OnActivateAsync();
        }
    }

    public class PairingState
    {
        public Dictionary<Guid, string> Cache { get; set; }
    }
}