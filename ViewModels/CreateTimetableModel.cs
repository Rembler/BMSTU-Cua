using System;
using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class CreateTimetableModel
    {
        [Required(ErrorMessage = "Timetable name not specified")]
        public string Name { get; set; }
        public int RoomId { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Timetable appointment duration not specified")]
        [Range(1, 180, ErrorMessage = "Время приема должно быть в пределах от 1 до 180 минут")]
        public int? AppointmentDuration { get; set; }

        [Required(ErrorMessage = "Timetable break duration not specified")]
        [Range(1, 180, ErrorMessage = "Время перерыва должно быть в пределах от 1 до 180 минут")]
        public int? BreakDuration { get; set; }
    }
}