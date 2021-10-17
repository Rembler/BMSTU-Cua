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
        public async Task<IActionResult> Create()
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            CreateRoomModel model = new CreateRoomModel() { Company = user.Company, About = "Generic room description" };
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateRoomModel model)
        {
            if (ModelState.IsValid)
            {
                User admin = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
                Room newRoom = new Room {
                    Name = model.Name, Company = model.Company,
                    About = model.About, Private = model.Private,
                    Hidden = model.Hidden, Admin = admin };
                db.Rooms.Add(newRoom);
                await db.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> Join()
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            List<Room> rooms = db.Rooms.Include(r => r.Admin).Include(r => r.Users).Where(r => r.Admin != user && !r.Users.Contains(user)).ToList();
            ViewBag.CurrentUser = user.Email;
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

        // [Route("/Room/{id}")]
        public IActionResult Content(int id)
        {
            Room room = db.Rooms.Include(r => r.Users).Include(r => r.Admin).FirstOrDefault(r => r.Id == id);
            return View(room);
        }

        public async Task<IActionResult> Queues(int id)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            ViewBag.User = user;
            List<Queue> queues = await db.Queues
                .Include(q => q.QueueUser)
                .ThenInclude(qu => qu.User)
                .Where(q => q.RoomId == id)
                .ToListAsync();
            return View(queues);
        }
    }
}