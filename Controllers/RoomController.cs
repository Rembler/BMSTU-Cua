using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Models;
using Cua.Services;
using Cua.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cua.Controllers
{
    [Authorize]
    public class RoomController : Controller
    {
        private ApplicationContext db;
        private readonly AuthorizationService _authorizer;
        
        public RoomController(ApplicationContext context, AuthorizationService authorizationService)
        {
            db = context;
            _authorizer = authorizationService;
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
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
            if (!_authorizer.IsAdmin(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin of this room"});

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

        public async Task<IActionResult> Update(int id, string newName, string newCompany, string newAbout)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

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
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            List<Room> rooms = db.Rooms
                .Include(r => r.Admin)
                .Include(r => r.RoomUsers)
                .Include(r => r.Queues)
                .AsSplitQuery()
                .Where(r => r.Admin != user && !r.RoomUsers.Any(ru => ru.UserId == user.Id))
                .ToList();
            List<Request> requests = db.Requests.Where(r => r.User == user && !r.FromRoom).ToList();
            ViewBag.Requests = requests;
            return View(rooms);
        }

        public async Task<IActionResult> Decline(int id, int userId)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            Request request = await db.Requests.FirstOrDefaultAsync(r => r.UserId == userId
                && r.RoomId == id
                && !r.Checked
                && !r.FromRoom);

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

        public async Task<IActionResult> AddUser(int id, int? userId)
        {
            var room = await db.Rooms.FindAsync(id);
            User user;

            if (userId != null)
            {
                if (!_authorizer.IsModerator(HttpContext, id))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

                user = await db.Users.FindAsync(userId);
            }
            else
                user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);

            if (user != null && room != null)
            {
                Request request = await db.Requests.FirstOrDefaultAsync(r => r.User == user
                    && r.Room == room
                    && !r.Checked);

                if (userId != null)
                {
                    if (request != null && request.FromRoom)
                    {
                        Console.Write("Room admin trying to accept request from room");
                        return Json(null);
                    }
                    if (request == null)
                    {
                        Console.Write("This user didn't send a join request");
                        return Json(null);
                    }
                }
                else
                {
                    if (request != null && !request.FromRoom)
                    {
                        Console.Write("User trying to accept request from user");
                        return Json(null);
                    }
                    if (request == null && room.Private)
                    {
                        Console.Write("User trying to enter private room without request");
                        return Json(null);
                    }
                }

                if (request != null)
                {
                    request.Checked = true;
                    db.Requests.Update(request);
                }

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

        public async Task<IActionResult> ChangeModeratorStatus(int id, int userId)
        {
            if (!_authorizer.IsAdmin(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin of this room"});

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

        public async Task<IActionResult> DeleteUser(int id, int userId)
        {
            if (!_authorizer.IsAdmin(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin of this room"});

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

        public async Task<IActionResult> AddRequest(int id, string comment, int? userId)
        {
            User user;
            bool fromRoom = false;
            if (userId != null)
            {
                if (!_authorizer.IsModerator(HttpContext, id))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});
            
                user = await db.Users.FindAsync(userId);
                fromRoom = true;
            }
            else
                user = await _authorizer.GetCurrentUserAsync(HttpContext);
            
            var room = await db.Rooms.FindAsync(id);
            if (user != null && room != null)
            {
                Request request = await db.Requests.FirstOrDefaultAsync(r => r.Room == room && r.User == user);

                if (request != null)
                {
                    request.Checked = false;
                    request.FromRoom = fromRoom;
                    request.Comment = comment;
                    db.Requests.Update(request);
                }
                else
                {
                    Request newRequest = new Request { 
                        Room = room, 
                        User = user, 
                        Checked = false, 
                        FromRoom = fromRoom, 
                        Comment = comment 
                    };
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
            if (!_authorizer.IsMember(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            RoomContentModel model = new RoomContentModel() {
                Room = db.Rooms
                    .Include(r => r.RoomUsers)
                        .ThenInclude(ru => ru.User)
                    .Include(r => r.Admin)
                    .Include(r => r.Queues)
                        .ThenInclude(q => q.QueueUsers)
                    .AsSplitQuery()
                    .OrderBy(r => r.Id)
                    .FirstOrDefault(r => r.Id == id),
                CurrentUser = _authorizer.GetCurrentUser(HttpContext)
            };
            model.Messages = db.Messages.Where(m => m.Room == model.Room).ToList();
            
            return View(model);
        }

        public IActionResult Control(int id)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});
            
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
                .Where(r => r.Room == model.Room 
                    && r.Checked == false 
                    && !r.FromRoom)
                .ToList();
            model.Queues = db.Queues
                .Include(q => q.QueueUsers)
                .Where(q => q.Room == model.Room && q.Creator == user)
                .ToList();
            return View(model);
        }

        public IActionResult Candidates(int id)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});
            
            return View();
        }

        public async Task<IActionResult> Queues(int id)
        {
            if (!_authorizer.IsMember(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            ViewBag.CurrentUser = user;
            List<Queue> queues = await db.Queues
                .Include(q => q.Creator)
                .Include(q => q.QueueUsers)
                .ThenInclude(qu => qu.User)
                .Where(q => q.RoomId == id)
                .ToListAsync();
            return View(queues);
        }

        public async Task<IActionResult> GetAvailableUsers(int id, string searchedName, string searchedCompany)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            if (searchedCompany == null)
                searchedCompany = "";
            if (searchedName == null)
                searchedName = "";

            if (searchedName != "" || searchedCompany != "")
            {
                Room room = await db.Rooms.FindAsync(id);
                if (room != null)
                {
                    List<User> availableUsers = await db.Users.Include(u => u.RoomUsers)
                        .Where(u => !u.RoomUsers.Any(ru => ru.RoomId == id)
                            && u.Id != room.AdminId
                            && (u.Name.Contains(searchedName) || u.Surname.Contains(searchedName))
                            && u.Company.Contains(searchedCompany))
                        .ToListAsync();

                    if (availableUsers != null)
                    {
                        List<AvailableUserModel> model = new List<AvailableUserModel>();
                        foreach (var item in availableUsers)
                        {
                            model.Add(new AvailableUserModel {
                                Name = item.Name + " " + item.Surname,
                                UserId = item.Id,
                                Company = item.Company
                            });
                        }
                        return Json(model);
                    }
                    return Json(0);
                }
                Console.Write("Room with ID = " + id + " doesn't exist");
                return Json(null);
            }
            return Json(0);
        }
    }
}