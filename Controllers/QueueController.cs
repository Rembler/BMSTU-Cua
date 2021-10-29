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
                return RedirectToAction("Content", "Room", new { id = model.RoomId });
            }
            return View(model);
        }

        public async Task<JsonResult> AddUser(int id)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == HttpContext.User.Identity.Name);
            // Room room = await db.Rooms.FindAsync(roomId);
            Queue queue = await db.Queues.FindAsync(id);
            List<QueueUser> alreadyIn = db.QueueUser.Where(qu => qu.Queue == queue).ToList();

            int place = alreadyIn.Any() ? alreadyIn.Max(qu => qu.Place) : 1;
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

        public JsonResult GetParticipants(int id)
        {
            List<QueueUser> QueueUser = db.QueueUser.Include(qu => qu.User).Where(qu => qu.QueueId == id).ToList();
            List<String> userNames = QueueUser.Select(qu => qu.User.Name + " " + qu.User.Surname).ToList();
            return Json(userNames);
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
    }
}