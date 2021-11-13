using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Models;
using Microsoft.EntityFrameworkCore;

namespace Cua.Services
{
    public class SharedHelperService
    {
        private ApplicationContext db;

        public SharedHelperService(ApplicationContext context)
        {
            db = context;
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
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveUserFromTimetableAsync(User user, Timetable timetable)
        {
            Appointment updatedAppointment = await db.Appointments.FirstOrDefaultAsync(a => a.UserId == user.Id
                && a.TimetableId == timetable.Id);

            if (updatedAppointment != null)
            {
                updatedAppointment.User = null;
                updatedAppointment.IsAvailable = true;
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
    }
}