using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using Cua.Models;
using Cua.Services;
using Cua.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace Cua.Controllers
{
    [Authorize]
    public class TimetableController : Controller
    {
        private readonly AuthorizationService _authorizer;
        private readonly ActivityHelperService _timetabledb;
        private readonly SharedHelperService _shareddb;
        
        public TimetableController(AuthorizationService authorizationService,
            ActivityHelperService activityHelperService,
            SharedHelperService sharedHelperService)
        {
            _authorizer = authorizationService;
            _timetabledb = activityHelperService;
            _shareddb = sharedHelperService;
        }

        [HttpGet]
        public IActionResult Create(int roomId)
        {
            if (!_authorizer.IsModerator(HttpContext, roomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            CreateTimetableModel model = new CreateTimetableModel() { RoomId = roomId };
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateTimetableModel model)
        {
            if (!_authorizer.IsModerator(HttpContext, model.RoomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the admin or moderator of this room"});

            if (ModelState.IsValid)
            {
                User creator = await _authorizer.GetCurrentUserAsync(HttpContext);
                Room room = await _shareddb.GetRoomAsync(model.RoomId);
                Timetable newTimetable = await _timetabledb.CreateTimetableAsync(model, creator, room);
                return RedirectToAction("AppointmentSettings", "Timetable", new { id = newTimetable.Id, newEndDate = "" });
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this timetable"});

            if(await _timetabledb.DeleteTimetableAsync(id))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> Update(int id, string newName)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this timetable"});

            if (await _timetabledb.UpdateTimetableAsync(id, newName))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> AddUser(int id, DateTime startAt)
        {
            Timetable timetable = await _timetabledb.GetTimetableAsync(id);

            if (!_authorizer.IsMember(HttpContext, timetable.RoomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            User user = _authorizer.GetCurrentUser(HttpContext);
            await _timetabledb.AddUserToTimetableAsync(user, timetable.Id, startAt);
            return Json("OK");
        }

        public async Task<IActionResult> RemoveUser(int id, int? userId)
        {
            Timetable timetable = await _timetabledb.GetTimetableAsync(id);
            User user;

            if (userId == null)
            {
                if (!_authorizer.IsMember(HttpContext, timetable.RoomId))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});
                user = await _authorizer.GetCurrentUserAsync(HttpContext);
            }
            else
            {
                if (!_authorizer.IsTimetableCreator(HttpContext, timetable.Id))
                    return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});
                user = await _shareddb.GetUserByIdAsync((int)userId);
            }

            if(await _shareddb.RemoveUserFromTimetableAsync(user, timetable))
                return Json("OK");
            else
                return Json(null);
        }

        public async Task<IActionResult> Settings(int id)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this timetable"});

            Timetable timetable = await _timetabledb.GetTimetableAsync(id);   
            return View(timetable);
        }

        [HttpGet]
        public async Task<IActionResult> AppointmentSettings(int id, string newEndDate)
        {
            Timetable timetable = await _timetabledb.GetTimetableAsync(id);
            int step = timetable.AppointmentDuration + timetable.BreakDuration;
            DateTime startTime = DateTime.Parse("00:00");
            DateTime endTime = DateTime.Parse("23:59");

            List<string> timeList = new List<string>();
            foreach(DateTime item in EachAppointment(startTime, endTime, step))
            {  
                timeList.Add(item.ToString("t"));  
            }

            ViewBag.NewEndDate = newEndDate;
            return View(timeList);
        }

        [HttpPost]
        public async Task<IActionResult> AppointmentSettings([FromBody] AppointmentSettingsModel model)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, model.TimetableId))
                return Json(new { redirectUrl = Url.Action("Warning", "Home", new { message = "You are not creator of this timetable" }) });

            Timetable timetable = await _timetabledb.GetTimetableAsync(model.TimetableId);
            DateTime dayStart = model.NewEndDate == "" ? timetable.StartDate : timetable.EndDate, dayEnd = dayStart;
            DateTime endDate = model.NewEndDate == "" ? timetable.EndDate : DateTime.ParseExact(model.NewEndDate, "dd-MM-yyyy", CultureInfo.InvariantCulture);
            int step = timetable.AppointmentDuration + timetable.BreakDuration;

            foreach(DateTime currentDay in EachDay(dayStart.AddDays(1), endDate))
            {  
                dayStart = dayEnd;
                dayEnd = currentDay;
                string weekDay = dayStart.DayOfWeek.ToString();

                int index = 0;
                DateTime startAt = dayStart, endAt = startAt;
                foreach(DateTime item in EachAppointment(dayStart.AddMinutes(step), dayEnd.AddMinutes(-1), step))
                {  
                    startAt = endAt;
                    endAt = item;
                    if (model.Days.FirstOrDefault(d => d.WeekDay == weekDay).Appointments[index] == 1)
                        await _timetabledb.AddAppointmentToTimetableAsync(timetable, startAt, endAt);
                    index++;
                }
            }

            if (model.NewEndDate != "")
                await _timetabledb.UpdateTimetableEndDateAsync(timetable, endDate);
            return Json(new { redirectUrl = Url.Action("Control", "Room", new { id = timetable.RoomId }) });
        }

        public async Task<JsonResult> GetAvailableDates(int id)
        {
            return Json(await _timetabledb.GetTimetableAvailableDatesAsync(id));
        }

        public async Task<JsonResult> GetAvailableTime(int id, DateTime date)
        {
            return Json(await _timetabledb.GetTimetableAvailableTimeAsync(id, date));
        }

        private IEnumerable<DateTime> EachAppointment(DateTime startTime, DateTime endTime, int step) {  
            for (var time = startTime; time <= endTime; time = time.AddMinutes(step)) yield  
            return time;  
        } 

        private IEnumerable<DateTime> EachDay(DateTime startDate, DateTime endDate) {  
            for (var date = startDate.Date; date.Date <= endDate.Date; date = date.AddDays(1)) yield  
            return date;  
        } 
    }
}