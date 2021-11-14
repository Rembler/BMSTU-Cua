using System.Collections.Generic;

namespace Cua.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Password { get; set; }
        public byte[] StoredSalt { get; set; }
        public string ConfirmationToken { get; set; }
        public bool IsConfirmed { get; set; }
        public ICollection<Room> AdminRooms { get; set; }
        public ICollection<RoomUser> RoomUsers { get; set; }
        public ICollection<Queue> CreatedQueues { get; set; }
        public ICollection<Timetable> CreatedTimetables { get; set; }
        public ICollection<QueueUser> QueueUsers { get; set; }
        public ICollection<Request> Requests { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public ICollection<Notification> Notifications { get; set; }
        public User()
        {
            CreatedQueues = new List<Queue>();
            CreatedTimetables = new List<Timetable>();
            RoomUsers = new List<RoomUser>();
            AdminRooms = new List<Room>();
            QueueUsers = new List<QueueUser>();
            Requests = new List<Request>();
            Appointments = new List<Appointment>();
            Notifications = new List<Notification>();
        }
    }
}