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
    public class QueueController : Controller
    {
        private readonly AuthorizationService _authorizer;
        private readonly ActivityHelperService _queuedb;
        private readonly SharedHelperService _shareddb;
        
        public QueueController(AuthorizationService authorizationService,
            ActivityHelperService activityHelperService,
            SharedHelperService sharedHelperService)
        {
            _authorizer = authorizationService;
            _queuedb = activityHelperService;
            _shareddb = sharedHelperService;
        }

        [HttpGet]
        public IActionResult Create(int roomId)
        {
            if (!_authorizer.IsModerator(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "Вы не админинстратор и не модератор этой комнаты"});

            CreateQueueModel model = new CreateQueueModel() { RoomId = roomId };
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQueueModel model)
        {
            if (!_authorizer.IsModerator(HttpContext, model.RoomId))
                return RedirectToAction("Warning", "Home", new { message = "Вы не админинстратор и не модератор этой комнаты"});

            if (ModelState.IsValid)
            {
                User creator = await _authorizer.GetCurrentUserAsync(HttpContext);
                await _queuedb.CreateQueueAsync(creator, model);
                return RedirectToAction("Control", "Room", new { id = model.RoomId });
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "Вы не создатель этой очереди"});

            if (await _queuedb.DeleteQueueAsync(id))
                return Json("OK");
            else
            {
                Console.Write("Can't find queue with ID = " + id);
                return Json(null);
            }
        }

        public async Task<IActionResult> Settings(int id)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "Вы не создатель этой очереди"});

            Queue queue = await _shareddb.GetQueueAsync(id);               
            return View(queue);
        }

        public async Task<IActionResult> Update(int id, string newName, int newLimit, DateTime newStartAt)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "Вы не создатель этой очереди"});

            if (await _queuedb.UpdateQueueAsync(id, newName, newLimit, newStartAt))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> AddUser(int id, int roomId)
        {
            if (!_authorizer.IsMember(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "Вы не участник этой комнаты"});

            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            if (await _shareddb.AddUserToQueueAsync(user, id))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> ChangeActiveStatus(int id)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "Вы не создатель этой очереди"});

            if (await _queuedb.ChangeQueueActiveStatusAsync(id))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> GetUsers(int id, int roomId)
        {
            if (!_authorizer.IsMember(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "Вы не участник этой комнаты"});

            Queue queue = await _shareddb.GetQueueAsync(id);
            
            if (queue != null)
            {
                if (queue.QueueUsers.Count() == 0)
                    return Json(0);
                else
                {
                    List<QueueMemberModel> queueMembers = new List<QueueMemberModel>();
                    foreach (var item in queue.QueueUsers)
                    {
                        queueMembers.Add(new QueueMemberModel { 
                            Name = item.User.Name + " " + item.User.Surname,
                            UserId = item.UserId,
                            Place = item.Place 
                        });
                    }
                    return Json(queueMembers); 
                }                   
            }
            Console.Write("Can't find queue with ID = " + id);
            return Json(null);
        }

        public async Task<IActionResult> RemoveUser(int id, int? userId)
        {
            if (userId != null)
            {
                if (!_authorizer.IsQueueCreator(HttpContext, id))
                    return RedirectToAction("Warning", "Home", new { message = "Вы не создатель этой очереди"});
            }
            else
                userId = (await _authorizer.GetCurrentUserAsync(HttpContext)).Id;

            User user = await _shareddb.GetUserByIdAsync((int)userId);
            Queue queue = await _shareddb.GetQueueAsync(id);
            
            if (await _shareddb.RemoveUserFromQueueAsync(user, queue))
                return Json("OK");
            else
                return Json(null);
        }
    }
}