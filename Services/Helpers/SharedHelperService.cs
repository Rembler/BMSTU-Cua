using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Hubs;
using Cua.Models;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Cua.Services
{
    public class SharedHelperService
    {
        private ApplicationContext db;
        private readonly IHubContext<ChatHub> _hub;

        public SharedHelperService(ApplicationContext context, IHubContext<ChatHub> hubContext)
        {
            db = context;
            _hub = hubContext;
        }

        public async Task<bool> RemoveUserFromQueueAsync(User user, Queue queue)
        {
            List<QueueUser> queueUsers = await db.QueueUsers.Where(qu => qu.QueueId == queue.Id).ToListAsync();
            QueueUser removedQueueUser = queueUsers.FirstOrDefault(qu => qu.User == user);
            if (removedQueueUser != null)
            {
                db.QueueUsers.Remove(removedQueueUser);
                queueUsers.Remove(removedQueueUser);

                foreach (var item in queueUsers.Where(qu => qu.Place > removedQueueUser.Place))
                    item.Place--;
                db.QueueUsers.UpdateRange(queueUsers);
                await db.SaveChangesAsync();

                if (removedQueueUser.Place == 1)
                {
                    QueueUser toNotify = await db.QueueUsers
                        .Include(qu => qu.Queue)
                        .Include(qu => qu.User)
                        .FirstOrDefaultAsync(qu => qu.Queue.Active 
                            && qu.Place == 1 
                            && qu.QueueId == removedQueueUser.QueueId);
                    if (toNotify != null)
                        await _hub.Clients.User(toNotify.User.Email).SendAsync("ReceiveNotification", "Вы следующий в очереди " + toNotify.Queue.Name);
                }

                HubGroup hubGroup = await db.HubGroups.Include(hg => hg.HubUsers).FirstOrDefaultAsync(hg => hg.Name == "queue-" + queue.Id);
                HubUser hubUser = await db.HubUsers.FindAsync(user.Email);
                hubGroup.HubUsers.Remove(hubUser);

                await _hub.Clients.All.SendAsync("ReceiveRemovalWish", queue.Id, removedQueueUser.Place - 1);
                
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> AddUserToQueueAsync(User user, int queueId)
        {
            Queue queue = await GetQueueAsync(queueId);
            int place = queue.QueueUsers.Any() ? queue.QueueUsers.Max(qu => qu.Place) + 1 : 1;

            if (user != null && queue != null)
            {
                if (queue.Limit == 0 || queue.Limit > queue.QueueUsers.Count())
                {
                    if (!queue.QueueUsers.Any(qu => qu.User == user))
                    {
                        QueueUser queueUser = new QueueUser() { User = user, Queue = queue, Place = place };
                        db.QueueUsers.Update(queueUser);

                        HubGroup hubGroup = await db.HubGroups.Include(hg => hg.HubUsers).FirstOrDefaultAsync(hg => hg.Name == "queue-" + queueId);
                        HubUser hubUser = await db.HubUsers.FindAsync(user.Email);
                        hubGroup.HubUsers.Add(hubUser);

                        await _hub.Clients.All.SendAsync("ReceiveAdditionWish", queue.Id, user.Name + " " + user.Surname, user.Id);

                        await db.SaveChangesAsync();
                        return true;
                    }
                }
            }
            return false;
        }

        public async Task<Queue> GetQueueAsync(int id)
        {
            return await db.Queues
                .Include(q => q.QueueUsers)
                .ThenInclude(qu => qu.User)
                .FirstOrDefaultAsync(q => q.Id == id);
        }

        public async Task<bool> RemoveUserFromTimetableAsync(User user, Timetable timetable)
        {
            Appointment updatedAppointment = await db.Appointments.FirstOrDefaultAsync(a => a.UserId == user.Id
                && a.TimetableId == timetable.Id);

            if (updatedAppointment != null)
            {
                updatedAppointment.User = null;
                if (DateTime.Now < updatedAppointment.StartAt)
                {
                    updatedAppointment.IsAvailable = true;
                    updatedAppointment.NotificationCount = 0;
                }
                else
                    updatedAppointment.NotificationCount = 2;
                db.Appointments.Update(updatedAppointment);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<User> GetUserByIdAsync(int id)
        {
            return await db.Users.FindAsync(id);
        }

        public async Task<Room> GetRoomAsync(int id)
        {
            return await db.Rooms.Include(r => r.Admin).FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task SendQueueNotificationsAsync()
        {
            List<Queue> queues = await db.Queues
                .Include(q => q.Creator)
                .Include(q => q.QueueUsers)
                .ThenInclude(qu => qu.User)
                .Where(q => q.NotificationCount < 2)
                .ToListAsync();

            foreach (var item in queues)
            {
                int timeLeft = (int)item.StartAt.Subtract(DateTime.Now).TotalMinutes;
                string msg = "";
                if (item.NotificationCount == 0 && timeLeft <= 30 && timeLeft > 0)
                {
                    msg = "Очередь \"" + item.Name + "\" начнется через " + timeLeft + " мин.";
                    Console.WriteLine(msg);                    
                    await _hub.Clients.Group("queue-" + item.Id).SendAsync("ReceiveNotification", msg);
                    item.NotificationCount++;
                }
                else if (timeLeft <= 0)
                {
                    msg = "Очередь \"" + item.Name + "\" уже началась";
                    Console.WriteLine(msg);
                    await _hub.Clients.Group("queue-" + item.Id).SendAsync("ReceiveNotification", msg);
                    item.NotificationCount = 2;
                    item.Active = true;
                }
                if (msg != "")
                {
                    foreach (var queueUser in item.QueueUsers)
                        await CreateNotificationAsync(queueUser.User, msg);
                    await CreateNotificationAsync(item.Creator, msg);
                }
                db.Queues.Update(item);
            }  
            await db.SaveChangesAsync(); 
        }

        public async Task SendTimetableNotificationsAsync()
        {
            List<Appointment> appointments = await db.Appointments
                .Include(a => a.Timetable)
                .ThenInclude(t => t.Creator)
                .Include(a => a.User)
                .Where(a => a.NotificationCount < 2 && a.UserId != null)
                .ToListAsync();

            foreach (var item in appointments)
            {
                int timeLeft = (int)item.StartAt.Subtract(DateTime.Now).TotalMinutes;
                string msg = "";
                if (item.NotificationCount == 0 && timeLeft <= 30 && timeLeft > 0)
                {
                    msg = "Прием по распианию \"" + item.Timetable.Name + "\" начнется через " + timeLeft + " мин.";
                    await _hub.Clients.User(item.Timetable.Creator.Email).SendAsync("ReceiveNotification", msg);
                    await _hub.Clients.User(item.User.Email).SendAsync("ReceiveNotification", msg);
                    Console.WriteLine(msg);
                    item.NotificationCount++;
                }
                else if (timeLeft <= 0)
                {
                    msg = "Прием по расписанию \"" + item.Timetable.Name + "\" уже начался";
                    await _hub.Clients.User(item.Timetable.Creator.Email).SendAsync("ReceiveNotification", msg);
                    await _hub.Clients.User(item.User.Email).SendAsync("ReceiveNotification", msg);
                    Console.WriteLine(msg);
                    item.NotificationCount = 2;
                }
                if (msg != "")
                {
                    await CreateNotificationAsync(item.User, msg);
                    await CreateNotificationAsync(item.Timetable.Creator, msg);
                }
                db.Appointments.Update(item);
            }
            await db.SaveChangesAsync();
        }

        public async Task UpdateOldAppointmentsAsync()
        {
            List<Appointment> updatedAppointments = await db.Appointments.Where(a => a.UserId == null
                && a.IsAvailable
                && a.StartAt < DateTime.Now).ToListAsync();
            foreach (var item in updatedAppointments)
                item.IsAvailable = false;
            db.Appointments.UpdateRange(updatedAppointments);
            await db.SaveChangesAsync();
        }

        public async Task CreateNotificationAsync(User user, string body)
        {
            Notification newNotification = new Notification() {
                Body = body,
                User = user,
                Closed = false,
                Date = DateTime.Now
            };
            db.Notifications.Add(newNotification);
            await db.SaveChangesAsync();
        }

        public async Task<List<Notification>> GetNotificationsAsync(User user)
        {
            return await db.Notifications.Where(n => n.UserId == user.Id && !n.Closed).ToListAsync();
        }

        public async Task UpdateNotificationAsync(int id)
        {
            Notification updatedNotification = await db.Notifications.FindAsync(id);
            updatedNotification.Closed = true;
            db.Notifications.Update(updatedNotification);
            await db.SaveChangesAsync();
        }
    }
}