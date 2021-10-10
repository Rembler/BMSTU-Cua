using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Cryptography.KeyDerivation;
using System.Threading.Tasks;
using Cua.Models;
using Cua.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Cua.Services;

namespace Cua.Controllers
{
    public class AccountController : Controller
    {
        private ApplicationContext db;
        private readonly IMailService _mailService;

        public class HashSalt
        {
            public string Hash { get; set; }
            public byte[] Salt { get; set; }
        }

        public AccountController(ApplicationContext context, IMailService mailService)
        {
            db = context;
            _mailService = mailService;
        }

        [HttpGet]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                //  проверка наличия пользователя в базе данных
                User user = await db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user != null)
                {
                    //  проверка пароля
                    var isPasswordMatched = VerifyPassword(model.Password, user.StoredSalt, user.Password);

                    if (isPasswordMatched)
                    {
                        if (!user.IsConfirmed)
                        {
                            ModelState.AddModelError("", "Вы не подтвердили свой email");
                            return View(model);
                        }
                        //  аутентификация пользователя
                        await Authenticate(model.Email);
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Некоректные логин и/или пароль");
            }
            return View(model);
        }

        [HttpGet]
        public IActionResult RestorePassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePassword(RestorePasswordModel model)
        {
            User user = await db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);

            if (user == null)
            {
                ModelState.AddModelError("", "Указан неверный адрес электронной почты");
                return View(model);
            }

            var callbackUrl = Url.Action(
                "ChangePassword", "Account",
                new { userId = user.Id, token = user.ConfirmationToken },
                protocol: HttpContext.Request.Scheme);

            await _mailService.SendEmailAsync(
                user.Email,
                "Восстановление пароля",
                $"Для восстановления пароля перейдите <a href='{callbackUrl}'>по ссылке</a>.");
            return Content($"Для восстановления пароля перейдите по ссылке из письма, отправленного на {user.Email}.");
        }

        [HttpGet]
        public async Task<IActionResult> ChangePassword(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Login", "Account");

            User user = await db.Users.FindAsync(Convert.ToInt32(userId));
            if (user == null)
                return RedirectToAction("Login", "Account");

            if (user.ConfirmationToken == token)
            {
                ChangePasswordModel model = new ChangePasswordModel { UserId = user.Id };
                return View(model);
            }
            else
                return RedirectToAction("Login", "Account");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            User user = await db.Users.FindAsync(model.UserId);
            
            HashSalt hashSalt = EncryptPassword(model.Password);
            user.Password = hashSalt.Hash;
            user.StoredSalt = hashSalt.Salt;
            db.Users.Update(user);
            await db.SaveChangesAsync();

            await Authenticate(user.Email);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                if (user == null)
                {
                    //  генерирация токена для подтверждения адреса электронной почты
                    string generatedToken = Convert.ToBase64String(Guid.NewGuid().ToByteArray());
                    //  хэширование пароля с помощью соли
                    var hashsalt = EncryptPassword(model.Password);
                    // добавление пользователя в бд
                    db.Users.Add(new User {
                        Name = model.Name, Surname = model.Surname,
                        Email = model.Email, Password = hashsalt.Hash,
                        StoredSalt = hashsalt.Salt, IsConfirmed = false,
                        ConfirmationToken = generatedToken });
                    await db.SaveChangesAsync();

                    var addedUser = await db.Users.FirstOrDefaultAsync(u => u.Email == model.Email);
                    //  формирование ссылки для подтверждения адреса электронной почты
                    var callbackUrl = Url.Action(
                        "ConfirmEmail", "Account",
                        new { userId = addedUser.Id, token = generatedToken },
                        protocol: HttpContext.Request.Scheme);
                    //  отправка письма
                    await _mailService.SendEmailAsync(
                        model.Email,
                        "Подтверждение адреса электронной почты",
                        $"Для завершения регистрации перейдите <a href='{callbackUrl}'>по ссылке</a>.");

                    return Content($"Для завершения регистрации перейдите по ссылке из письма, отправленного на {model.Email}.");
                }
                ModelState.AddModelError("", "Пользователь с такой почтой уже зарегистрирован");
                return View(model);
            }
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return View("Error");

            var user = await db.Users.FindAsync(Convert.ToInt32(userId));
            if (user == null)
                return View("Error");
            //  проверка токенов на соответствие
            if (user.ConfirmationToken == token)
            {
                user.IsConfirmed = true;
                db.Users.Update(user);
                await db.SaveChangesAsync();
                //  аутентификация пользователя
                await Authenticate(user.Email);
                return RedirectToAction("Index", "Home");
            }
            else
                return View("Error");
        }

        private HashSalt EncryptPassword(string password)
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

        private bool VerifyPassword(string givenPassword, byte[] salt, string storedPassword)
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
 
        private async Task Authenticate(string userName)
        {
            // создание одного claim'а
            var claims = new List<Claim>
            {
                new Claim(ClaimsIdentity.DefaultNameClaimType, userName)
            };
            // создание объекта ClaimsIdentity
            ClaimsIdentity id = new ClaimsIdentity(claims, "ApplicationCookie", ClaimsIdentity.DefaultNameClaimType, ClaimsIdentity.DefaultRoleClaimType);
            // установка аутентификационных куки
            await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(id));
        }
 
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}