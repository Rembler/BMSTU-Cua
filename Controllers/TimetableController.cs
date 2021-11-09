using System;
using System.Collections.Generic;
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
        private ApplicationContext db;
        private readonly AuthorizationService _authorizer;
        
        public TimetableController(ApplicationContext context, AuthorizationService authorizationService)
        {
            db = context;
            _authorizer = authorizationService;
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
                Room room = await db.Rooms.FindAsync(model.RoomId);
                Timetable newTimetable = new Timetable() {
                    Name = model.Name,
                    Room = room,
                    Creator = creator,
                    StartDate = model.StartDate,
                    EndDate = model.EndDate,
                    AppointmentDuration = model.AppointmentDuration ?? default(int),
                    BreakDuration = model.BreakDuration ?? default(int)
                };
                db.Timetables.Add(newTimetable);
                await db.SaveChangesAsync();
                return RedirectToAction("AppointmentSettings", "Timetable", new { id = newTimetable.Id });
            }
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this timetable"});

            Timetable removedTimetable = await db.Timetables.FindAsync(id);

            if (removedTimetable != null)
            {
                db.Timetables.Remove(removedTimetable);
                await db.SaveChangesAsync();
                return Json("OK");
            }
            Console.Write("Can't find timetable with ID = " + id);
            return Json(null);
        }

        [HttpGet]
        public IActionResult AppointmentSettings(int id)
        {
            Timetable timetable = db.Timetables.Find(id);
            int step = timetable.AppointmentDuration + timetable.BreakDuration;
            DateTime startTime = DateTime.Parse("00:00");
            DateTime endTime = DateTime.Parse("23:59");
            List<string> timeList = new List<string>();
            foreach(DateTime item in EachAppointment(startTime, endTime, step))
            {  
                timeList.Add(item.ToString("t"));  
            }
            return View(timeList);
        }

        [HttpPost]
        public IActionResult AppointmentSettings([FromBody] AppointmentSettingsModel model)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, model.TimetableId))
                return Json(new { redirectUrl = Url.Action("Warning", "Home", new { message = "You are not creator of this timetable" }) });

            Timetable timetable = db.Timetables.Find(model.TimetableId);
            DateTime dayStart = timetable.StartDate, dayEnd = dayStart;
            int step = timetable.AppointmentDuration + timetable.BreakDuration;

            foreach(DateTime currentDay in EachDay(dayStart.AddDays(1), timetable.EndDate))
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
                    
                    Appointment newAppointment = new Appointment() {
                        StartAt = startAt,
                        EndAt = endAt,
                        Timetable = timetable,
                        IsAvailable = model.Days.FirstOrDefault(d => d.WeekDay == weekDay).Appointments[index] == 1
                    };
                    db.Appointments.Add(newAppointment);

                    index++;
                }
            }
            db.SaveChanges();
            return Json(new { redirectUrl = Url.Action("Control", "Room", new { id = timetable.RoomId }) });
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