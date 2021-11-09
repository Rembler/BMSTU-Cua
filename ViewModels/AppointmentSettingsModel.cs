using System.Collections.Generic;

namespace Cua.ViewModels
{
    public class AppointmentSettingsModel
    {
        public int TimetableId { get; set; }
        public List<DayModel> Days { get; set; }
    }

    public class DayModel
    {
        public string WeekDay { get; set; }
        public List<int> Appointments { get; set; }
    }
}