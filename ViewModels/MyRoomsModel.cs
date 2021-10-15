using System.Collections.Generic;
using Cua.Models;

namespace Cua.ViewModels
{
    public class MyRoomsModel
    {
        public List<Room> myRooms { get; set; }
        public List<Room> notMyRooms { get; set; }
        public User currentUser { get; set; }
    }
}