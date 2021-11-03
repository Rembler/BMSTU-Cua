using Cua.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace Cua.Services
{
    public class AuthorizationService
    {
        private ApplicationContext db;

        public AuthorizationService(ApplicationContext context)
        {
            db = context;
        }

        public async Task<User> GetCurrentUserAsync(HttpContext httpContext)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.Email == httpContext.User.Identity.Name);
        }

        public User GetCurrentUser(HttpContext httpContext)
        {
            return db.Users.FirstOrDefault(u => u.Email == httpContext.User.Identity.Name);
        }

        public bool IsAdmin(HttpContext httpContext, int roomId)
        {
            Room room = db.Rooms.Find(roomId);
            User user = GetCurrentUser(httpContext);

            if (room.AdminId == user.Id)
                return true;
            else
                return false;
        }

        public bool IsModerator(HttpContext httpContext, int roomId)
        {
            Room room = db.Rooms.Include(r => r.RoomUsers).FirstOrDefault(r => r.Id == roomId);
            User user = GetCurrentUser(httpContext);

            if (room.RoomUsers.Where(ru => ru.IsModerator).Any(ru => ru.UserId == user.Id) || IsAdmin(httpContext, roomId))
                return true;
            else
                return false;
        }

        public bool IsMember(HttpContext httpContext, int roomId)
        {
            Room room = db.Rooms.Include(r => r.RoomUsers).FirstOrDefault(r => r.Id == roomId);
            User user = GetCurrentUser(httpContext);

            if (room.RoomUsers.Any(ru => ru.UserId == user.Id) || IsAdmin(httpContext, roomId))
                return true;
            else
                return false;
        }

        public bool IsCreator(HttpContext httpContext, int queueId)
        {
            Queue queue = db.Queues.Find(queueId);
            User user = GetCurrentUser(httpContext);

            if (queue.CreatorId == user.Id)
                return true;
            else
                return false;
        }
    }
}