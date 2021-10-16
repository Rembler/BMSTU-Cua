using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Models;
using Cua.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cua.Controllers
{
    public class RoomController : Controller
    {
        private ApplicationContext db;
        
        public RoomController(ApplicationContext context)
        {
            db = context;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoomModel model)
        {
            if (ModelState.IsValid)
            {
                User admin = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
                Room newRoom = new Room { Name = model.Name, AdminId = admin.Id };
                newRoom.Users.Add(admin);
                db.Rooms.Add(newRoom);
                await db.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> Join()
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            List<Room> rooms = db.Rooms.Include(r => r.Users).Where(r => !r.Users.Contains(user)).ToList();
            // Console.WriteLine(user.Email);
            // Console.Write(rooms.Count());
            return View(rooms);
        }

        public async Task<JsonResult> AddUser(int id)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            var room = await db.Rooms.FindAsync(id);
            if (user != null && room != null)
            {
                // var roomUsers = db.Users.Where(u => u.Rooms.Any(r => r.Id == id)).ToList();
                if (!room.Users.Contains(user))
                {
                    room.Users.Add(user);
                    db.Rooms.Update(room);
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User already in the room");
                return Json(null);
            }
            Console.Write("No such user or room");
            return Json(null);
        }
    }
}