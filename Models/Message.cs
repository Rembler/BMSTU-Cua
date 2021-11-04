using System;
using System.Collections.Generic;

namespace Cua.Models
{
    public class Message
    {
        public int Id { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public string Sender { get; set; }
        public string Body { get; set; }
    }
}