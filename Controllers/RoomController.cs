using Microsoft.AspNetCore.Mvc;
using SketchIt.Models;

namespace SketchIt.Controllers
{
    public class RoomController : Controller
    {
        private readonly RoomStore _roomStore;

        public RoomController(RoomStore roomStore)
        {
            _roomStore = roomStore;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        public IActionResult Lobby(string code, string name)
        {
            ViewBag.Code = code;
            ViewBag.Name = name;
            return View();
        }
        private string GenerateRoomCode()
        {
            return Guid.NewGuid().ToString("N")[..4].ToUpper();
        }
    }
}
