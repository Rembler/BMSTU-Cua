using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Models;
using Cua.ViewModels;
using Microsoft.EntityFrameworkCore;

namespace Cua.Services
{
    public class RoomHelperService
    {
        private ApplicationContext db;
        private readonly SharedHelperService shared;

        public RoomHelperService(ApplicationContext context, SharedHelperService sharedHelperService)
        {
            db = context;
            shared = sharedHelperService;
        }

        public async Task CreateRoomAsync(User user, CreateRoomModel model)
        {
            Room newRoom = new Room {
                Name = model.Name, Company = model.Company,
                About = model.About, Private = model.Private,
                Hidden = model.Hidden, Admin = user };
            db.Rooms.Add(newRoom);
            await db.SaveChangesAsync();
        }

        public async Task<bool> DeleteRoomAsync(int id)
        {
            Room removedRoom = await shared.GetRoomAsync(id);
            if (removedRoom != null)
            {
                db.Rooms.Remove(removedRoom);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<bool> UpdateRoomAsync(int id, string name, string company, string about)
        {
            Room room = await shared.GetRoomAsync(id);
            if (room != null)
            {
                room.Name = name;
                room.Company = company;
                room.About = about;

                db.Rooms.Update(room);
                await db.SaveChangesAsync();

                return true;
            }
            return false;
        }

        public async Task<List<Room>> GetAvailableRoomsAsync(User user)
        {
            return await db.Rooms
                .Include(r => r.Admin)
                .Include(r => r.RoomUsers)
                .Include(r => r.Queues)
                .AsSplitQuery()
                .Where(r => r.Admin != user && !r.RoomUsers.Any(ru => ru.UserId == user.Id))
                .ToListAsync();
        }

        public async Task<bool> DeclineRoomRequestAsync(int userId, int roomId)
        {
            Request request = await GetRequestAsync(userId, roomId, false);
            if (request != null && !request.FromRoom)
            {
                request.Checked = true;
                db.Requests.Update(request);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task<Request> GetRequestAsync(int userId, int roomId, bool checkedStatus)
        {
            return await db.Requests.FirstOrDefaultAsync(r => r.UserId == userId
                && r.RoomId == roomId
                && r.Checked == checkedStatus);
        }

        public async Task<RoomUser> GetRoomUserAsync(User user, Room room)
        {
            return await db.RoomUsers.FirstOrDefaultAsync(ru => ru.Room == room && ru.User == user);
        }

        public async Task AddUserToRoomAsync(User user, Room room)
        {
            RoomUser newRoomUser = new RoomUser() { 
                Room = room, 
                User = user, 
                IsModerator = false
            };
            db.RoomUsers.Add(newRoomUser);
            await db.SaveChangesAsync();
        }

        public async Task<bool> UpdateModeratorStatusAsync(User user, Room room)
        {
            RoomUser roomUser = await GetRoomUserAsync(user, room);
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
                return true;
            }
            return false;
        }

        public async Task<bool> RemoveUserFromRoomAsync(User user, Room room)
        {
            RoomUser removedRoomUser = await GetRoomUserAsync(user, room);
            if (removedRoomUser != null)
            {
                List<Timetable> updatedTimetables = await db.Timetables.Where(t => t.RoomId == room.Id).ToListAsync();
                foreach (var item in updatedTimetables)
                    await shared.RemoveUserFromTimetableAsync(user, item);

                List<Queue> updatedQueues = await db.Queues.Where(q => q.RoomId == room.Id).ToListAsync();
                foreach (var item in updatedQueues)
                    await shared.RemoveUserFromQueueAsync(user, item);
                
                db.RoomUsers.Remove(removedRoomUser);
                await db.SaveChangesAsync();
                return true;
            }
            return false;           
        }

        public async Task AddRequestAsync(User user, Room room, string comment, bool isFromRoom)
        {
            Request request = await GetRequestAsync(user.Id, room.Id, true);
            if (request != null)
            {
                request.Checked = false;
                request.FromRoom = isFromRoom;
                request.Comment = comment;
                db.Requests.Update(request);
            }
            else
            {
                Request newRequest = new Request { 
                    Room = room, 
                    User = user, 
                    Checked = false, 
                    FromRoom = isFromRoom, 
                    Comment = comment 
                };
                db.Requests.Add(newRequest);
            }
            await db.SaveChangesAsync();
        }

        public async Task<Room> GetFullRoomInfoAsync(int id)
        {
            return await db.Rooms
                .Include(r => r.RoomUsers)
                .ThenInclude(ru => ru.User)
                .Include(r => r.Admin)
                .Include(r => r.Queues)
                .ThenInclude(q => q.QueueUsers)
                .AsSplitQuery()
                .OrderBy(r => r.Id)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<List<Message>> GetMessagesAsync(int id)
        {
            return await db.Messages.Where(m => m.RoomId == id).ToListAsync();
        }

        public async Task<ControlPanelModel> GetControlPanelModelAsync(int roomId, User user)
        {
            return new ControlPanelModel() {
                CurrentUser = user,
                Room = await db.Rooms
                    .Include(r => r.Admin)
                    .Include(r => r.RoomUsers)
                    .ThenInclude(ru => ru.User)
                    .FirstOrDefaultAsync(r => r.Id == roomId),
                Requests = await db.Requests
                    .Include(r => r.User)
                    .Where(r => r.RoomId == roomId 
                        && r.Checked == false 
                        && !r.FromRoom)
                    .ToListAsync(),
                Queues = await db.Queues
                    .Include(q => q.QueueUsers)
                    .Where(q => q.RoomId == roomId && q.Creator == user)
                    .ToListAsync(),
                Timetables = await db.Timetables
                    .Include(t => t.Appointments)
                    .Where(t => t.RoomId == roomId && t.Creator == user)
                    .ToListAsync()
            };
        }

        public async Task<ActivitiesModel> GetRoomActivitiesAsync(int roomId, User user)
        {
            return new ActivitiesModel() {
                CurrentUser = user,
                Queues = await db.Queues
                    .Include(q => q.Creator)
                    .Include(q => q.QueueUsers)
                    .ThenInclude(qu => qu.User)
                    .Where(q => q.RoomId == roomId)
                    .ToListAsync(),
                Timetables = await db.Timetables
                    .Include(t => t.Creator)
                    .Include(t => t.Appointments)
                    .Where(t => t.RoomId == roomId)
                    .ToListAsync()
            };
        }

        public async Task<List<User>> GetAvailableUsersAsync(Room room, string name, string company)
        {
            return await db.Users
                .Include(u => u.RoomUsers)
                .Where(u => !u.RoomUsers.Any(ru => ru.RoomId == room.Id)
                    && u.Id != room.AdminId
                    && (u.Name.Contains(name)
                        || u.Surname.Contains(name)
                        || (u.Name + " " + u.Surname).Contains(name)
                        || (u.Surname + " " + u.Name).Contains(name))
                    && u.Company.Contains(company))
                .ToListAsync();
        }

        public async Task UpdateRequestAsync(Request request)
        {
            request.Checked = true;
            db.Requests.Update(request);
            await db.SaveChangesAsync();
        }

        public async Task<List<Request>> GetRequestsFromUserAsync(User user)
        {
            return await  db.Requests.Where(r => r.User == user && !r.FromRoom).ToListAsync();
        }
    }
}