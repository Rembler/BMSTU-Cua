using System.Collections.Generic;
using Cua.Models;

namespace Cua.ViewModels
{
    public class ControlPanelModel
    {
        public Room Room { get; set; }
        public User CurrentUser { get; set; }
        public List<Request> Requests { get; set; }
        public List<Queue> Queues { get; set; }
    }
}