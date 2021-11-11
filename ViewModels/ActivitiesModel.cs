using System.Collections.Generic;
using Cua.Models;

namespace Cua.ViewModels
{
    public class ActivitiesModel
    {
        public List<Queue> Queues { get; set; }
        public List<Timetable> Timetables { get; set; }
        public User CurrentUser { get; set; }
    }
}