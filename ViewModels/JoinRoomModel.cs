using System.Collections.Generic;
using Cua.Models;

namespace Cua.ViewModels
{
    public class JoinRoomModel
    {
        public List<Room> Rooms { get; set; }
        public List<Request> Requests { get; set; }
    }
}