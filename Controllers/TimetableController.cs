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
                return RedirectToAction("AppointmentSettings", "Timetable", new { id = newTimetable.Id, newEndDate = "" });
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

        public async Task<IActionResult> Update(int id, string newName)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this timetable"});

            Timetable updatedTimetable = await db.Timetables.FindAsync(id);

            if (updatedTimetable != null)
            {
                updatedTimetable.Name = newName;

                db.Timetables.Update(updatedTimetable);
                await db.SaveChangesAsync();

                return Json("OK");
            }
            return Json(null);
        }

        public IActionResult Schedule(int id)
        {
            Timetable timetable = db.Timetables.Include(t => t.Appointments).FirstOrDefault(t => t.Id == id);
            return View(timetable);
        }

        public IActionResult AddUser(int id, DateTime startAt)
        {
            Timetable timetable = db.Timetables.Find(id);

            if (!_authorizer.IsMember(HttpContext, timetable.RoomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            Appointment appointment = db.Appointments.FirstOrDefault(a => a.TimetableId == timetable.Id && a.StartAt == startAt);
            User user = _authorizer.GetCurrentUser(HttpContext);

            appointment.User = user;
            appointment.IsAvailable = false;
            db.Appointments.Update(appointment);
            db.SaveChanges();

            return Json(new { redirectUrl = Url.Action("Activities", "Room", new { id = timetable.RoomId }) });
        }

        public async Task<IActionResult> RemoveUser(int id, int? appointmentId)
        {
            Timetable timetable = await db.Timetables.FindAsync(id);

            if (!_authorizer.IsMember(HttpContext, timetable.RoomId))
                return RedirectToAction("Warning", "Home", new { message = "You are not the member of this room"});

            Appointment appointment;
            if (appointmentId == null)
            {
                User user = await _authorizer.GetCurrentUserAsync(HttpContext);
                appointment = await db.Appointments.FirstOrDefaultAsync(a => a.TimetableId == timetable.Id && a.UserId == user.Id);
            }
            else
            {
                appointment = await db.Appointments.Include(a => a.User).FirstOrDefaultAsync(a => a.Id == appointmentId);
            }

            appointment.User = null;
            appointment.IsAvailable = true;
            db.Appointments.Update(appointment);
            await db.SaveChangesAsync();

            return Json("OK");
        }

        public async Task<IActionResult> Settings(int id)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, id))
                return RedirectToAction("Warning", "Home", new { message = "You are not creator of this timetable"});

            Timetable timetable = await db.Timetables
                .Include(t => t.Appointments)
                .ThenInclude(a => a.User)
                .FirstOrDefaultAsync(t => t.Id == id);
                
            return View(timetable);
        }

        [HttpGet]
        public IActionResult AppointmentSettings(int id, string newEndDate)
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

            ViewBag.NewEndDate = newEndDate;
            return View(timeList);
        }

        [HttpPost]
        public IActionResult AppointmentSettings([FromBody] AppointmentSettingsModel model)
        {
            if (!_authorizer.IsTimetableCreator(HttpContext, model.TimetableId))
                return Json(new { redirectUrl = Url.Action("Warning", "Home", new { message = "You are not creator of this timetable" }) });

            Console.Write(" " + model.NewEndDate);
            Timetable timetable = db.Timetables.Find(model.TimetableId);
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
                    {
                        Appointment newAppointment = new Appointment() {
                            StartAt = startAt,
                            EndAt = endAt,
                            Timetable = timetable,
                            IsAvailable = true
                        };
                        db.Appointments.Add(newAppointment);
                    }

                    index++;
                }
            }

            if (model.NewEndDate != "")
            {
                timetable.EndDate = endDate;
                db.Timetables.Update(timetable);
            }

            db.SaveChanges();
            return Json(new { redirectUrl = Url.Action("Control", "Room", new { id = timetable.RoomId }) });
        }

        public JsonResult GetAvailableDates(int id)
        {
            List<string> availableDates = db.Appointments
                .Where(a => a.TimetableId == id && a.IsAvailable)
                .Select(a => a.StartAt.ToString("dd-MM-yyyy"))
                .Distinct()
                .ToList();
            return Json(availableDates);
        }

        public JsonResult GetAvailableTime(int id, DateTime date)
        {
            List<string> availableTime = db.Appointments
                .Where(a => a.TimetableId == id 
                    && a.StartAt.Date == date.Date 
                    && a.IsAvailable)
                .Select(a => a.StartAt.ToString("HH:mm")
                    + " - "
                    + a.StartAt.AddMinutes(a.Timetable.AppointmentDuration).ToString("HH:mm"))
                .ToList();
            return Json(availableTime);
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