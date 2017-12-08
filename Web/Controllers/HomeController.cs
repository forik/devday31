using System;
using System.Threading.Tasks;
using GrainInterfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Orleans;

namespace Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly IClusterClient _client;

        public HomeController(IClusterClient client)
        {
            _client = client;
        }

        private Guid GetGuid()
        {
            var playerId = HttpContext.Session.GetString("playerId");
            if (playerId != null)
            {
                return Guid.Parse(playerId);
            }

            var guid = Guid.NewGuid();
            HttpContext.Session.SetString("playerId", guid.ToString());
            return guid;
        }

        public class ViewModel
        {
            public string GameId { get; set; }
        }

        public ActionResult Index(Guid? id)
        {
            var vm = new ViewModel {GameId = (id.HasValue) ? id.Value.ToString() : ""};
            return View(vm);
        }

        public async Task<ActionResult> Join(Guid id)
        {
            var guid = GetGuid();
            var player = _client.GetGrain<IPlayerGrain>(guid);
            var state = await player.JoinGame(id);

            return RedirectToAction("Index", id);
        }
    }
}