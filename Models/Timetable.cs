using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Cua.Models
{
    public class Timetable
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public int CreatorId { get; set; }
        public User Creator { get; set; }
        
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }
        public int AppointmentDuration { get; set; }
        public int BreakDuration { get; set; }
        public ICollection<Appointment> Appointments { get; set; }
        public Timetable()
        {
            Appointments = new List<Appointment>();
        }
    }
}