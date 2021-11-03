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

        public async Task<IActionResult> Delete(int id) 
        {
            Room removedRoom = await db.Rooms.FindAsync(id);
            if (removedRoom != null)
            {
                db.Rooms.Remove(removedRoom);
                await db.SaveChangesAsync();
                return RedirectToAction("Index", "Home");
            }
            Console.Write("Can't find room with ID = " + id);
            return RedirectToAction("Control", "Room");
        }

        public async Task<JsonResult> Update(int id, string newName, string newCompany, string newAbout)
        {
            Room room = await db.Rooms.FindAsync(id);
            if (room != null)
            {
                room.Name = newName;
                room.Company = newCompany;
                room.About = newAbout;

                db.Rooms.Update(room);
                await db.SaveChangesAsync();

                return Json("OK");
            }
            return Json(null);
        }

        public async Task<IActionResult> Join()
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            List<Room> rooms = db.Rooms
                .Include(r => r.Admin)
                .Include(r => r.RoomUsers)
                .Include(r => r.Queues)
                .AsSplitQuery()
                .Where(r => r.Admin != user && !r.RoomUsers.Any(ru => ru.UserId == user.Id))
                .ToList();
            List<Request> requests = db.Requests.Where(r => r.User == user).ToList();
            ViewBag.Requests = requests;
            return View(rooms);
        }

        public async Task<JsonResult> Decline(int id, int userId)
        {
            Request request = await db.Requests.FirstOrDefaultAsync(r => r.UserId == userId && r.RoomId == id);
            if (request != null)
            {
                request.Checked = true;
                db.Requests.Update(request);
                await db.SaveChangesAsync();
                return Json("OK");
            }
            Console.Write("Request not found");
            return Json(null);
        }

        public async Task<JsonResult> AddUser(int id, int? userId)
        {
            var room = await db.Rooms.FindAsync(id);
            User user;

            if (userId != null)
            {
                user = await db.Users.FindAsync(userId);
                Request request = await db.Requests.FirstOrDefaultAsync(r => r.User == user && r.Room == room);
                request.Checked = true;
                db.Requests.Update(request);
            }
            else
            {
                user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            }

            if (user != null && room != null)
            {
                // var roomUsers = db.Users.Where(u => u.Rooms.Any(r => r.Id == id)).ToList();
                // if (!room.Users.Contains(user))
                if (!db.RoomUsers.Any(ru => ru.Room == room && ru.User == user))
                {
                    RoomUser newRoomUser = new RoomUser() { 
                        Room = room, 
                        User = user, 
                        IsModerator = false
                    };
                    db.RoomUsers.Add(newRoomUser);
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User already in the room");
                return Json(null);
            }
            Console.Write("No such user or room");
            return Json(null);
        }

        public async Task<JsonResult> ChangeModeratorStatus(int id, int userId)
        {
            User user = await db.Users.FindAsync(userId);
            Room room = await db.Rooms.Include(r => r.Admin).FirstOrDefaultAsync(r => r.Id == id);

            if (user != null && room != null)
            {
                RoomUser roomUser = await db.RoomUsers.FirstOrDefaultAsync(ru => ru.User == user && ru.Room == room);
                if (roomUser != null)
                {
                    roomUser.IsModerator = !roomUser.IsModerator;
                    if (!roomUser.IsModerator)
                    {
                        List<Queue> changedQueues = await db.Queues
                            .Include(q => q.Creator)
                            .Where(q => q.Creator == user)
                            .ToListAsync();
                        foreach (var item in changedQueues)
                            item.Creator = room.Admin;
                        db.Queues.UpdateRange(changedQueues);
                    }
                    db.RoomUsers.Update(roomUser);
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User isn't a member of the room");
                return Json(null);
            }
            Console.Write($"User with ID = {user.Id} or room with ID = {room.Id} doesn't exist");
            return Json(null);
        }

        public async Task<JsonResult> DeleteUser(int id, int userId)
        {
            User user = await db.Users.FindAsync(userId);
            Room room = await db.Rooms.FindAsync(id);

            if (user != null && room != null)
            {
                RoomUser removedRoomUser = await db.RoomUsers.FirstOrDefaultAsync(ru => ru.User == user && ru.Room == room);
                if (removedRoomUser != null)
                {
                    db.RoomUsers.Remove(removedRoomUser);
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User isn't a member of the room");
                return Json(null);
            }
            Console.Write($"User with ID = {user.Id} or room with ID = {room.Id} doesn't exist");
            return Json(null);
        }

        public async Task<JsonResult> AddRequest(int id, string comment)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            var room = await db.Rooms.FindAsync(id);
            if (user != null && room != null)
            {
                Request request = await db.Requests.FirstOrDefaultAsync(r => r.Room == room && r.User == user);
                if (request != null)
                {
                    request.Checked = false;
                    request.Comment = comment;
                    db.Requests.Update(request);
                }
                else
                {
                    Request newRequest = new Request { Room = room, User = user, Checked = false, Comment = comment };
                    db.Requests.Add(newRequest);
                }
                await db.SaveChangesAsync();
                return Json("OK");
            }
            Console.Write("No such user or room");
            return Json(null);
        }

        // [Route("/Room/{id}")]
        public IActionResult Content(int id)
        {
            Room room = db.Rooms
                .Include(r => r.RoomUsers)
                    .ThenInclude(ru => ru.User)
                .Include(r => r.Admin)
                .Include(r => r.Queues)
                    .ThenInclude(q => q.QueueUsers)
                .AsSplitQuery()
                .OrderBy(r => r.Id)
                .FirstOrDefault(r => r.Id == id);
            ViewBag.CurrentUser = db.Users.FirstOrDefault(u => u.Email == HttpContext.User.Identity.Name);
            return View(room);
        }

        public IActionResult Control(int id)
        {
            User user = db.Users.FirstOrDefault(u => u.Email == HttpContext.User.Identity.Name);
            ControlPanelModel model = new ControlPanelModel();
            model.CurrentUser = user;
            model.Room = db.Rooms
                .Include(r => r.Admin)
                .Include(r => r.RoomUsers)
                    .ThenInclude(ru => ru.User)
                .FirstOrDefault(r => r.Id == id);
            model.Requests = db.Requests
                .Include(r => r.User)
                .Where(r => r.Room == model.Room && r.Checked == false)
                .ToList();
            model.Queues = db.Queues
                .Include(q => q.QueueUsers)
                .Where(q => q.Room == model.Room && q.Creator == user)
                .ToList();
            return View(model);
        }

        public async Task<IActionResult> Queues(int id)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            ViewBag.CurrentUser = user;
            List<Queue> queues = await db.Queues
                .Include(q => q.Creator)
                .Include(q => q.QueueUsers)
                .ThenInclude(qu => qu.User)
                .Where(q => q.RoomId == id)
                .ToListAsync();
            return View(queues);
        }
    }
}