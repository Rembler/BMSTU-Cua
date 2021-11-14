using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Cua.Models;
using Microsoft.EntityFrameworkCore;

namespace Cua.Services
{
    public class UserHelperService
    {
        private ApplicationContext db;
        private readonly SharedHelperService shared;

        public UserHelperService(ApplicationContext context, SharedHelperService sharedHelperService)
        {
            db = context;
            shared = sharedHelperService;
        }

        public async Task<User> GetUserByEmailAsync(string email)
        {
            return await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        }

        public async Task<User> UpdateUserPasswordAsync(int id, AuthorizationService.HashSalt hashSalt)
        {
            User user = await shared.GetUserByIdAsync(id);
            user.Password = hashSalt.Hash;
            user.StoredSalt = hashSalt.Salt;
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<User> CreateUserAsync(ViewModels.RegisterModel model, AuthorizationService.HashSalt hashSalt)
        {
            string generatedToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
            db.Users.Add(new User {
                Name = model.Name, Surname = model.Surname,
                Email = model.Email, Password = hashSalt.Hash,
                Company = model.Company, StoredSalt = hashSalt.Salt, 
                IsConfirmed = false, ConfirmationToken = generatedToken });
            await db.SaveChangesAsync();
            return await GetUserByEmailAsync(model.Email);
        }

        public async Task DeleteUserAsync(User user)
        {
            List<Room> removedRooms = db.Rooms.Where(r => r.Admin == user).ToList();
            db.Rooms.RemoveRange(removedRooms);
            db.Users.Remove(user);

            HubUser hubUser = await db.HubUsers.FindAsync(user.Email);
            db.HubUsers.Remove(hubUser);

            await db.SaveChangesAsync();
        }

        public async Task<List<Request>> GetUserRequestsAsync(User user)
        {
            List<Request> requests = await db.Requests
                .Include(rq => rq.Room)
                .ThenInclude(r => r.Admin)
                .Where(rq=> rq.UserId == user.Id
                    && !rq.Checked
                    && rq.FromRoom)
                .ToListAsync();
            return requests;
        }

        public async Task<bool> DeclineUserRequestAsync(int roomId, User user)
        {
            Request request = await db.Requests.FirstOrDefaultAsync(rq => rq.UserId == user.Id
                && rq.RoomId == roomId
                && rq.FromRoom
                && !rq.Checked);
            
            if (request != null)
            {
                request.Checked = true;
                db.Requests.Update(request);
                await db.SaveChangesAsync();
                return true;
            }
            return false;
        }

        public async Task UpdateUserInfoAsync(string name, string surname, string company, User user)
        {
            user.Name = name;
            user.Surname = surname;
            user.Company = company;
            db.Users.Update(user);
            await db.SaveChangesAsync();
        }

        public async Task<User> UpdateUserEmailAsync(string email, User user)
        {
            user.Email = email;
            user.IsConfirmed = false;
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return user;
        }

        public async Task<User> ConfirmUserEmailAsync(User user)
        {
            user.IsConfirmed = true;
            db.Users.Update(user);
            await db.SaveChangesAsync();
            return user;
        }
    }
}