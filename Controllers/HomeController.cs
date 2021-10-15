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

namespace Cua.Controllers
{
    public class HomeController : Controller
    {
        private ApplicationContext db;
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger, ApplicationContext context)
        {
            _logger = logger;
            db = context;
        }

        [Authorize]
        public async Task<IActionResult> Index()
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            List<Room> myRoomsList = db.Rooms.Include(r => r.Users).Where(r => r.AdminId == user.Id).ToList();
            List<Room> notMyRoomsList = db.Rooms.Include(r => r.Users).Where(r => r.AdminId != user.Id && r.Users.Contains(user)).ToList();
            MyRoomsModel allRooms = new MyRoomsModel() { 
                myRooms = myRoomsList, 
                notMyRooms = notMyRoomsList, 
                currentUser = user
            };
            return View(allRooms);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
