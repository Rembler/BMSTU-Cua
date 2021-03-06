using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Hubs;
using Cua.Models;
using Cua.ViewModels;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace Cua.Services
{
    public class ActivityHelperService
    {
        private ApplicationContext db;
        private readonly SharedHelperService shared;

        public ActivityHelperService(ApplicationContext context, SharedHelperService sharedHelperService)
        {
            db = context;
            shared = sharedHelperService;
        }

        public async Task CreateQueueAsync(User user, CreateQueueModel model)
        {
            Room room = await db.Rooms.FindAsync(model.RoomId);
            Queue queue = new Queue() {
                Name = model.Name,
                Limit = model.Limit == null ? 0 : (int)model.Limit,
                StartAt = model.StartAt, 
                Room = room,
                Creator = user,
                Active = false,
                NotificationCount = 0
            };
            db.Queues.Add(queue);
            await db.SaveChangesAsync();

            HubGroup hubGroup = new HubGroup() { Name = "queue-" + queue.Id, HubUsers = new List<HubUser>() };
            db.HubGroups.Add(hubGroup);
            HubUser hubUser = await db.HubUsers.FindAsync(user.Email);
            hubGroup.HubUsers.Add(hubUser);

            await db.SaveChangesAsync();
        }

        public async Task<List<QueueUser>> GetQueueUsersAsync(int id)
        {
            return await db.QueueUsers.Where(qu => qu.QueueId == id).ToListAsync();
        }

        public async Task<bool> DeleteQueueAsync(int id)
        {
            Queue removedQueue = await shared.GetQueueAsync(id);
            if (removedQueue != null)
            {
                List<QueueUser> removedQueueUsers = await GetQueueUsersAsync(id);
                db.QueueUsers.RemoveRange(removedQueueUsers);
                db.Queues.Remove(removedQueue);

                HubGroup hubGroup = await db.HubGroups.FindAsync("queue-" + id);
                db.HubGroups.Remove(hubGroup);

                await db.SaveChangesAsync();                
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateQueueAsync(int id, string name, int limit, DateTime startAt)
        {
            Queue updatedQueue = await shared.GetQueueAsync(id);
            if (updatedQueue != null)
            {
                updatedQueue.Name = name;
                updatedQueue.Limit = limit > 0 ? limit : 0;
                if (updatedQueue.StartAt != startAt)
                    updatedQueue.NotificationCount = 0;
                updatedQueue.StartAt = startAt;

                db.Queues.Update(updatedQueue);
                await db.SaveChangesAsync();

                return true;
            }
            return false;
        }

        public async Task<bool> ChangeQueueActiveStatusAsync(int id)
        {
            Queue queue = await shared.GetQueueAsync(id);
            if (queue != null)
            {
                queue.Active = !queue.Active;
                db.Queues.Update(queue);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<Timetable> CreateTimetableAsync(CreateTimetableModel model, User user, Room room)
        {
            Timetable newTimetable = new Timetable() {
                Name = model.Name,
                Room = room,
                Creator = user,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                AppointmentDuration = model.AppointmentDuration ?? default(int),
                BreakDuration = model.BreakDuration ?? default(int)
            };
            db.Timetables.Add(newTimetable);
            await db.SaveChangesAsync();
            return newTimetable;
        }

        public async Task<Timetable> GetTimetableAsync(int id)
        {
            return await db.Timetables
                .Include(t => t.Appointments)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == id);
        }

        public async Task<bool> DeleteTimetableAsync(int id)
        {
            Timetable removedTimetable = await GetTimetableAsync(id);
            if (removedTimetable != null)
            {
                db.Timetables.Remove(removedTimetable);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateTimetableAsync(int id, string name)
        {
            Timetable updatedTimetable = await GetTimetableAsync(id);
            if (updatedTimetable != null)
            {
                updatedTimetable.Name = name;
                db.Timetables.Update(updatedTimetable);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task AddUserToTimetableAsync(User user, int timetableId, DateTime startAt)
        {
            Appointment appointment = await db.Appointments.FirstOrDefaultAsync(a => a.TimetableId == timetableId && a.StartAt == startAt);
            appointment.User = user;
            appointment.IsAvailable = false;
            db.Appointments.Update(appointment);
            await db.SaveChangesAsync();
        }

        public async Task AddAppointmentToTimetableAsync(Timetable timetable, DateTime startAt, DateTime endAt)
        {
            Appointment newAppointment = new Appointment() {
                StartAt = startAt,
                EndAt = endAt,
                Timetable = timetable,
                IsAvailable = true,
                NotificationCount = 0
            };
            db.Appointments.Add(newAppointment);
            await db.SaveChangesAsync();
        }

        public async Task UpdateTimetableEndDateAsync(Timetable timetable, DateTime endDate)
        {
            timetable.EndDate = endDate;
            db.Timetables.Update(timetable);
            await db.SaveChangesAsync();
        }

        public async Task<List<string>> GetTimetableAvailableDatesAsync(int id)
        {
            return await db.Appointments
                .Where(a => a.TimetableId == id && a.IsAvailable)
                .Select(a => a.StartAt.ToString("dd-MM-yyyy"))
                .Distinct()
                .ToListAsync();
        }

        public async Task<List<string>> GetTimetableAvailableTimeAsync(int id, DateTime date)
        {
            return await db.Appointments
                .Where(a => a.TimetableId == id 
                    && a.StartAt.Date == date.Date 
                    && a.IsAvailable)
                .Select(a => a.StartAt.ToString("HH:mm")
                    + " - "
                    + a.StartAt.AddMinutes(a.Timetable.AppointmentDuration).ToString("HH:mm"))
                .ToListAsync();
        }
    }
}