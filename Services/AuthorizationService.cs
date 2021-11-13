using Cua.Models;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Security.Cryptography;
using System;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Collections.Generic;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Cua.Services
{
    public class AuthorizationService
    {
        private ApplicationContext db;

        public AuthorizationService(ApplicationContext context)
        {
            db = context;
        }

        public class HashSalt
        {
            public string Hash { get; set; }
            public byte[] Salt { get; set; }
        }

        public HashSalt EncryptPassword(string password)
        {
            byte[] salt = new byte[128 / 8];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(salt);
            }
            //  хэширование пароля
            string encryptedPassword = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: password,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 100,
                numBytesRequested: 256 / 8
            ));

            return new HashSalt { Hash = encryptedPassword, Salt = salt };
        }

        public bool VerifyPassword(string givenPassword, byte[] salt, string storedPassword)
        {
            string encryptedPassw = Convert.ToBase64String(KeyDerivation.Pbkdf2(
                password: givenPassword,
                salt: salt,
                prf: KeyDerivationPrf.HMACSHA1,
                iterationCount: 100,
                numBytesRequested: 256 / 8
            ));

            return encryptedPassw == storedPassword;
        }

        public async Task Authenticate(HttpContext httpContext, string userName)
        {
            // создание одного claim'а
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };
            // создание объекта ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await httpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
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

        public bool IsQueueCreator(HttpContext httpContext, int queueId)
        {
            Queue queue = db.Queues.Find(queueId);
            User user = GetCurrentUser(httpContext);

            if (queue.CreatorId == user.Id)
                return true;
            else
                return false;
        }

        public bool IsTimetableCreator(HttpContext httpContext, int timetableId)
        {
            Timetable timetable = db.Timetables.Find(timetableId);
            User user = GetCurrentUser(httpContext);

            if (timetable.CreatorId == user.Id)
                return true;
            else
                return false;
        }

    }
}