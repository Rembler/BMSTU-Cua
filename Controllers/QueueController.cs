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
        private ApplicationContext db;
        private readonly AuthorizationService _authorizer;
        
        public QueueController(ApplicationContext context, AuthorizationService authorizationService)
        {
            db = context;
            _authorizer = authorizationService;
        }

        [HttpGet]
        public IActionResult Create(int roomId)
        {
            if (!_authorizer.IsModerator(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            CreateQueueModel model = new CreateQueueModel() { RoomId = roomId };
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQueueModel model)
        {
            if (!_authorizer.IsModerator(HttpContext, model.RoomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            if (ModelState.IsValid)
            {
                User creator = await _authorizer.GetCurrentUserAsync(HttpContext);
                Room room = await db.Rooms.FindAsync(model.RoomId);
                Queue queue = new Queue() {
                    Name = model.Name,
                    Limit = model.Limit == null ? 0 : (int)model.Limit,
                    StartAt = model.StartAt, 
                    Room = room,
                    Creator = creator,
                    Active = false
                };
                db.Queues.Add(queue);
                await db.SaveChangesAsync();
                return RedirectToAction("Control", "Room", new { id = model.RoomId });
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this queue"});

            Queue removedQueue = await db.Queues.FindAsync(id);
            if (removedQueue != null)
            {
                List<QueueUser> removedQueueUsers = await db.QueueUsers.Where(qu => qu.QueueId == id).ToListAsync();
                db.QueueUsers.RemoveRange(removedQueueUsers);
                db.Queues.Remove(removedQueue);
                await db.SaveChangesAsync();
                
                return Json("OK");
            }
            Console.Write("Can't find queue with ID = " + id);
            return Json(null);
        }

        public async Task<IActionResult> Settings(int id)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this queue"});

            Queue queue = await db.Queues
                .Include(q => q.QueueUsers)
                    .ThenInclude(qu => qu.User)
                .FirstOrDefaultAsync(q => q.Id ==id);
                
            return View(queue);
        }

        public async Task<IActionResult> Update(int id, string newName, int newLimit, DateTime newStartAt)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this queue"});

            Queue updatedQueue = await db.Queues.FindAsync(id);

            if (updatedQueue != null)
            {
                updatedQueue.Name = newName;
                updatedQueue.Limit = newLimit > 0 ? newLimit : 0;
                updatedQueue.StartAt = newStartAt;

                db.Queues.Update(updatedQueue);
                await db.SaveChangesAsync();

                return Json("OK");
            }
            return Json(null);
        }

        public async Task<IActionResult> Join(int id, int roomId)
        {
            if (!_authorizer.IsMember(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            Queue queue = await db.Queues.FindAsync(id);
            List<QueueUser> alreadyIn = db.QueueUsers.Where(qu => qu.Queue == queue).ToList();

            int place = alreadyIn.Any() ? alreadyIn.Max(qu => qu.Place) + 1 : 1;
            if (user != null && queue != null)
            {
                // var roomUsers = db.Users.Where(u => u.Rooms.Any(r => r.Id == id)).ToList();
                if (!queue.QueueUsers.Any(qu => qu.User == user))
                {
                    QueueUser queueUser = new QueueUser() { User = user, Queue = queue, Place = place };
                    db.QueueUsers.Update(queueUser);
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User already in the queue");
                return Json(null);
            }
            Console.Write("No such user or queue");
            return Json(null);
        }

        public IActionResult ChangeActiveStatus(int id)
        {
            if (!_authorizer.IsQueueCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this queue"});

            Queue queue = db.Queues.Find(id);

            if (queue != null)
            {
                queue.Active = !queue.Active;
                db.Queues.Update(queue);
                db.SaveChanges();
                return Json("OK");
            }
            else
            {
                Console.Write("Can't find queue with ID = " + id);
                return Json(null);
            }
        }

        public async Task<IActionResult> GetUsers(int id, int roomId)
        {
            if (!_authorizer.IsMember(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            Queue queue = await db.Queues
                .Include(q => q.QueueUsers)
                .ThenInclude(qu => qu.User)
                .FirstOrDefaultAsync(q => q.Id == id);
            
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
            List<QueueUser> queueUsers = await db.QueueUsers.Where(qu => qu.QueueId == id).ToListAsync();

            if (queueUsers != null)
            {
                QueueUser removedUser;
                if (userId != null)
                {
                    if (!_authorizer.IsQueueCreator(HttpContext, id))
                        return RedirectToAction("Warning", "Home", new { message = "You are not creator of this queue"});

                    removedUser = queueUsers.FirstOrDefault(qu => qu.UserId == userId);
                }
                else
                {
                    User user = await _authorizer.GetCurrentUserAsync(HttpContext);
                    removedUser = queueUsers.FirstOrDefault(qu => qu.UserId == user.Id);
                }

                if (removedUser != null)
                {
                    db.QueueUsers.Remove(removedUser);
                    queueUsers.Remove(removedUser);

                    foreach (var item in queueUsers.Where(qu => qu.Place > removedUser.Place))
                        item.Place--;
                    db.QueueUsers.UpdateRange(queueUsers);
                    
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User with ID = " + userId + " is not in the queue");
                return Json(null);
            }
            Console.Write("Queue with ID = " + id + " doesn't have any members");
            return Json(null);
        }
    }
}