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
    public class QueueController : Controller
    {
        private ApplicationContext db;
        
        public QueueController(ApplicationContext context)
        {
            db = context;
        }

        [HttpGet]
        public IActionResult Create(int roomId)
        {
            CreateQueueModel model = new CreateQueueModel() { RoomId = roomId };
            return View(model);
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateQueueModel model)
        {
            if (ModelState.IsValid)
            {
                User creator = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
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

        public async Task<JsonResult> Delete(int id)
        {
            Queue removedQueue = await db.Queues.FindAsync(id);
            if (removedQueue != null)
            {
                List<QueueUser> removedQueueUsers = await db.QueueUser.Where(qu => qu.QueueId == id).ToListAsync();
                db.QueueUser.RemoveRange(removedQueueUsers);
                db.Queues.Remove(removedQueue);
                await db.SaveChangesAsync();
                
                return Json("OK");
            }
            Console.Write("Can't find queue with ID = " + id);
            return Json(null);
        }

        public async Task<JsonResult> Join(int id)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            // Room room = await db.Rooms.FindAsync(roomId);
            Queue queue = await db.Queues.FindAsync(id);
            List<QueueUser> alreadyIn = db.QueueUser.Where(qu => qu.Queue == queue).ToList();

            int place = alreadyIn.Any() ? alreadyIn.Max(qu => qu.Place) + 1 : 1;
            if (user != null && queue != null)
            {
                // var roomUsers = db.Users.Where(u => u.Rooms.Any(r => r.Id == id)).ToList();
                if (!queue.QueueUser.Any(qu => qu.User == user))
                {
                    QueueUser queueUser = new QueueUser() { User = user, Queue = queue, Place = place };
                    db.QueueUser.Update(queueUser);
                    await db.SaveChangesAsync();
                    return Json("OK");
                }
                Console.Write("User already in the queue");
                return Json(null);
            }
            Console.Write("No such user or queue");
            return Json(null);
        }

        public JsonResult ChangeActiveStatus(int id)
        {
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

        public async Task<JsonResult> GetUsers(int id)
        {
            Queue queue = await db.Queues
                .Include(q => q.QueueUser)
                .ThenInclude(qu => qu.User)
                .FirstOrDefaultAsync(q => q.Id == id);
            
            if (queue != null)
            {
                if (queue.QueueUser.Count() == 0)
                    return Json(0);
                else
                {
                    List<QueueMemberModel> queueMembers = new List<QueueMemberModel>();
                    foreach (var item in queue.QueueUser)
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

        public async Task<JsonResult> RemoveUser(int id, int? userId)
        {
            List<QueueUser> queueUsers = await db.QueueUser.Where(qu => qu.QueueId == id).ToListAsync();

            if (queueUsers != null)
            {
                QueueUser removedUser;
                if (userId != null)
                    removedUser = queueUsers.FirstOrDefault(qu => qu.UserId == userId);
                else
                {
                    User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
                    removedUser = queueUsers.FirstOrDefault(qu => qu.UserId == user.Id);
                }

                if (removedUser != null)
                {
                    db.QueueUser.Remove(removedUser);
                    queueUsers.Remove(removedUser);

                    foreach (var item in queueUsers)
                        item.Place--;
                    db.QueueUser.UpdateRange(queueUsers);
                    
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