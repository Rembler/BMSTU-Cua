using System;
using System.Collections.Generic;

namespace Cua.Models
{
    public class Request
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public Room Room { get; set; }
        public User User { get; set; }
        public bool Checked { get; set; }
        public bool FromRoom { get; set; }
        public string Comment { get; set; }
    }
}