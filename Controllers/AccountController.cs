using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Cua.Models;
using Cua.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Cua.Services;
using Microsoft.AspNetCore.Authorization;

namespace Cua.Controllers
{
    [Authorize]
    public class AccountController : Controller
    {
        private readonly MailService _mailService;
        private readonly AuthorizationService _authorizer;
        private readonly UserHelperService _userdb;
        private readonly SharedHelperService _shareddb;

        public AccountController(MailService mailService,
            AuthorizationService authorizationService,
            UserHelperService userHelperService,
            SharedHelperService sharedHelperService)
        {
            _mailService = mailService;
            _authorizer = authorizationService;
            _userdb = userHelperService;
            _shareddb = sharedHelperService;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }
        
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel model)
        {
            if (ModelState.IsValid)
            {
                //  проверка наличия пользователя в базе данных
                User user = await _userdb.GetUserByEmailAsync(model.Email);
                if (user != null)
                {
                    //  проверка пароля
                    var isPasswordMatched = _authorizer.VerifyPassword(model.Password, user.StoredSalt, user.Password);

                    if (isPasswordMatched)
                    {
                        if (!user.IsConfirmed)
                        {
                            ModelState.AddModelError("", "Вы не подтвердили свой email");
                            return View(model);
                        }
                        //  аутентификация пользователя
                        await _authorizer.Authenticate(HttpContext, model.Email);
                        return RedirectToAction("Index", "Home");
                    }
                }
                ModelState.AddModelError("", "Некоректные логин и/или пароль");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult RestorePassword()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RestorePassword(RestorePasswordModel model)
        {
            User user = await _userdb.GetUserByEmailAsync(model.Email);

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
            return RedirectToAction("Warning", "Home", new { message = $"Для восстановления пароля перейдите по ссылке из письма, отправленного на {user.Email}."});
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ChangePassword(string userId, string token)
        {
            if (userId == null || token == null)
                return RedirectToAction("Login", "Account");

            User user = await _shareddb.GetUserByIdAsync(Convert.ToInt32(userId));
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
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordModel model)
        {
            if (ModelState.IsValid)
            {
                var hashSalt = _authorizer.EncryptPassword(model.Password);
                User user = await _userdb.UpdateUserPasswordAsync(model.UserId, hashSalt);
                await _authorizer.Authenticate(HttpContext, user.Email);
                return RedirectToAction("Index", "Home");
            }
            return View(model);
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Register()
        {
            return View();
        }
        
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                User user = await _userdb.GetUserByEmailAsync(model.Email);
                if (user == null)
                {
                    var hashsalt = _authorizer.EncryptPassword(model.Password);
                    User newUser = await _userdb.CreateUserAsync(model, hashsalt);

                    var callbackUrl = Url.Action(
                        "ConfirmEmail", "Account",
                        new { userId = newUser.Id, token = newUser.ConfirmationToken },
                        protocol: HttpContext.Request.Scheme);

                    await _mailService.SendEmailAsync(
                        model.Email,
                        "Подтверждение адреса электронной почты",
                        $"Для завершения регистрации перейдите <a href='{callbackUrl}'>по ссылке</a>.");
                        
                    return RedirectToAction("Warning", "Home", new { message = $"Для завершения регистрации перейдите по ссылке из письма, отправленного на {newUser.Email}." });
                }
                ModelState.AddModelError("", "Пользователь с такой почтой уже зарегистрирован");
                return View(model);
            }
            return View(model);
        }

        public async Task<IActionResult> Delete()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            await _userdb.DeleteUserAsync(user);
            return RedirectToAction("Logout", "Account");
        }

        public async Task<IActionResult> Settings()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);

            RegisterModel model = new RegisterModel() { 
                Email = user.Email, Name = user.Name,
                Surname = user.Surname, Company = user.Company,
                Password = user.Password };
            
            return View(model);
        }

        public async Task<IActionResult> Requests()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            List<Request> requests =await _userdb.GetUserRequestsAsync(user);
            return View(requests);
        }

        public async Task<IActionResult> Decline(int roomId)
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            bool done = await _userdb.DeclineUserRequestAsync(roomId, user);
            if (done)
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<JsonResult> UpdateInfo(string name, string surname, string company)
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            await _userdb.UpdateUserInfoAsync(name, surname, company, user);
            return Json("OK");
        }

        public async Task<JsonResult> UpdateEmail(string email)
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);

            if (user.Email == email)
            {
                Console.Write("Новая электронная почта совпадает со старой");
                return Json(null);
            } 
            else
            {
                User existingUser = await _userdb.GetUserByEmailAsync(email);
                if (existingUser != null)
                {
                    Console.Write("Пользователь с почтой " + email + " уже существует");
                    return Json(null);
                }
                else
                {
                    user = await _userdb.UpdateUserEmailAsync(email, user);

                    var callbackUrl = Url.Action(
                        "ConfirmEmail", "Account",
                        new { userId = user.Id, token = user.ConfirmationToken },
                        protocol: HttpContext.Request.Scheme);
                        
                    await _mailService.SendEmailAsync(
                        user.Email,
                        "Подтверждение адреса электронной почты",
                        $"Для подтверждения нового адреса электронной почты перейдите <a href='{callbackUrl}'>по ссылке</a>.");

                    return Json("OK");
                }
            }
        }

        public async Task<JsonResult> UpdatePassword(string password)
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            var hashSalt = _authorizer.EncryptPassword(password);
            await _userdb.UpdateUserPasswordAsync(user.Id, hashSalt);
            return Json("OK");
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ConfirmEmail(string userId, string token)
        {
            if (userId == null || token == null)
                return View("Error");

            User user = await _shareddb.GetUserByIdAsync(Convert.ToInt32(userId));

            if (user == null)
                return View("Error");

            if (user.ConfirmationToken == token)
            {
                user = await _userdb.ConfirmUserEmailAsync(user);
                await _authorizer.Authenticate(HttpContext, user.Email);
                return RedirectToAction("Index", "Home");
            }
            else
                return View("Error");
        }

        public async Task<JsonResult> GetNotifications()
        {
            User user = await _authorizer.GetCurrentUserAsync(HttpContext);
            return Json(await _shareddb.GetNotificationsAsync(user));
        }

        public async Task<JsonResult> HideNotification(int id)
        {
            await _shareddb.UpdateNotificationAsync(id);
            return Json("OK");
        }
 
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Login", "Account");
        }
    }
}