using System.Collections.Generic;
using Cua.Models;

namespace Cua.ViewModels
{
    public class RoomContentModel
    {
        public Room Room { get; set; }
        public User CurrentUser { get; set; }
        public List<Message> Messages { get; set; }
    }
}