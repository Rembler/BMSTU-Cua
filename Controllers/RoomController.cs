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
        private readonly AuthorizationService _authorizer;
        private readonly RoomHelperService _roomdb;
        private readonly SharedHelperService _shareddb;
        
        public RoomController(AuthorizationService authorizationService,
            RoomHelperService roomHelperService,
            SharedHelperService sharedHelperService)
        {
            _authorizer = authorizationService;
            _roomdb = roomHelperService;
            _shareddb = sharedHelperService;
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
                User admin = await _authorizer.GetCurrentUserAsync(HttpContext);
                await _roomdb.CreateRoomAsync(admin, model);
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id) 
        {
            if (!_authorizer.IsAdmin(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin of this room"});

            if (await _roomdb.DeleteRoomAsync(id))
                return RedirectToAction("Index", "Home");
            else
            {
                Console.Write("Can't find room with ID = " + id);
                return RedirectToAction("Control", "Room");
            }
        }

        public async Task<IActionResult> Update(int id, string newName, string newCompany, string newAbout)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            if (await _roomdb.UpdateRoomAsync(id, newName, newCompany, newAbout))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> Join()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            JoinRoomModel model = new JoinRoomModel() {
                Rooms = await _roomdb.GetAvailableRoomsAsync(user),
                Requests = await _roomdb.GetRequestsFromUserAsync(user)
            };
            return View(model);
        }

        public async Task<IActionResult> Decline(int id, int userId)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            if (await _roomdb.DeclineRoomRequestAsync(userId, id))
                return Json("OK");
            else
            {
                Console.Write("Request not found");
                return Json(null);
            }
        }

        public async Task<IActionResult> AddUser(int id, int? userId)
        {
            var room = await _shareddb.GetRoomAsync(id);
            User user;

            if (userId != null)
            {
                if (!_authorizer.IsModerator(HttpContext, id))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

                user = await _shareddb.GetUserByIdAsync((int)userId);
            }
            else
                user = await _authorizer.GetCurrentUserAsync(HttpContext);

            if (user != null && room != null)
            {
                Request request = await _roomdb.GetRequestAsync(user.Id, room.Id, false);

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
                    await _roomdb.UpdateRequestAsync(request);

                RoomUser roomUser = await _roomdb.GetRoomUserAsync(user, room);
                if (roomUser == null)
                {
                    await _roomdb.AddUserToRoomAsync(user, room);
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

            User user = await _shareddb.GetUserByIdAsync(userId);
            Room room = await _shareddb.GetRoomAsync(id);

            if (user != null && room != null)
            {
                if (await _roomdb.UpdateModeratorStatusAsync(user, room))
                    return Json("OK");
                else
                {
                    Console.Write("User isn't a member of the room");
                    return Json(null);
                }
            }
            Console.Write($"User with ID = {user.Id} or room with ID = {room.Id} doesn't exist");
            return Json(null);
        }

        public async Task<IActionResult> RemoveUser(int id, int? userId)
        {
            User user;
            if (userId != null)
            {
                if (!_authorizer.IsAdmin(HttpContext, id))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the admin of this room"});

                user = await _shareddb.GetUserByIdAsync((int)userId);
            }
            else
            {
                if (!_authorizer.IsMember(HttpContext, id))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

                user = await _authorizer.GetCurrentUserAsync(HttpContext);
            }
            Room room = await _shareddb.GetRoomAsync(id);

            if (user != null && room != null)
            {
                if (await _roomdb.RemoveUserFromRoomAsync(user, room))
                {
                    if (userId != null)
                        return Json("OK");
                    else
                        return RedirectToAction("Index", "Home");
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
            
                user = await _shareddb.GetUserByIdAsync((int)userId);
                fromRoom = true;
            }
            else
                user = await _authorizer.GetCurrentUserAsync(HttpContext);
            
            var room = await _shareddb.GetRoomAsync(id);
            if (user != null && room != null)
            {
                await _roomdb.AddRequestAsync(user, room, comment, fromRoom);
                return Json("OK");
            }
            Console.Write("No such user or room");
            return Json(null);
        }

        public async Task<IActionResult> Content(int id)
        {
            if (!_authorizer.IsMember(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            RoomContentModel model = new RoomContentModel() {
                CurrentUser = await _authorizer.GetCurrentUserAsync(HttpContext),
                Room = await _roomdb.GetFullRoomInfoAsync(id),
                Messages = await _roomdb.GetMessagesAsync(id)
            };
            
            return View(model);
        }

        public async Task<IActionResult> Control(int id)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});
            
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            ControlPanelModel model = await _roomdb.GetControlPanelModelAsync(id, user);       
            return View(model);
        }

        public IActionResult Candidates(int id)
        {
            if (!_authorizer.IsModerator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});
            
            return View();
        }

        public async Task<IActionResult> Activities(int id)
        {
            if (!_authorizer.IsMember(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            ActivitiesModel model = await _roomdb.GetRoomActivitiesAsync(id, user);
            return View(model);
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
                Room room = await _shareddb.GetRoomAsync(id);
                if (room != null)
                {
                    List<User> availableUsers = await _roomdb.GetAvailableUsersAsync(room, searchedName, searchedCompany);

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