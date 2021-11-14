using System;

namespace Cua.Models
{
    public class Appointment
    {
        public int Id { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int NotificationCount { get; set; }
        public int? UserId { get; set; }
        public User User { get; set; }
        public int TimetableId { get; set; }
        public Timetable Timetable { get; set; }
        public bool IsAvailable { get; set; }
    }
}