using System;
using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class CreateTimetableModel
    {
        [Required(ErrorMessage = "Не указано название расписания")]
        public string Name { get; set; }
        public int RoomId { get; set; }

        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        [Required(ErrorMessage = "Не указана длительность приема")]
        [Range(1, 180, ErrorMessage = "Время приема должно быть в пределах от 1 до 180 минут")]
        public int? AppointmentDuration { get; set; }

        [Required(ErrorMessage = "Не указана длительность перерыва")]
        [Range(1, 180, ErrorMessage = "Время перерыва должно быть в пределах от 1 до 180 минут")]
        public int? BreakDuration { get; set; }
    }
}