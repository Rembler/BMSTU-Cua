using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Cua.Models;
using Microsoft.AspNetCore.Authorization;
using Cua.ViewModels;
using Microsoft.EntityFrameworkCore;
using Cua.Services;

namespace Cua.Controllers
{
    [Authorize]
    public class HomeController : Controller
    {
        private ApplicationContext db;
        private readonly ILogger<HomeController> _logger;
        private readonly AuthorizationService _authorizer;

        public HomeController(ILogger<HomeController> logger, ApplicationContext context, AuthorizationService authorizationService)
        {
            _logger = logger;
            db = context;
            _authorizer = authorizationService;
        }

        public async Task<IActionResult> Index()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            List<Room> roomsList = db.Rooms
                .Include(r => r.Admin)
                .Include(r => r.RoomUsers)
                .Include(r => r.Queues)
                .Include(r => r.Timetables)
                .AsSplitQuery()
                .Where(r => r.Admin == user)
                .ToList();
            roomsList = roomsList.Concat(db.Rooms
                .Include(r => r.Admin)
                .Include(r => r.RoomUsers)
                .Include(r => r.Queues)
                .Include(r => r.Timetables)
                .AsSplitQuery()
                .Where(r => r.RoomUsers.Any(ru => ru.UserId == user.Id))
                .ToList()).ToList();
            ViewBag.CurrentUser = user.Email;
            return View(roomsList);
        }

        [AllowAnonymous]
        public IActionResult Warning(string message)
        {
            return View((Object)message);
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
