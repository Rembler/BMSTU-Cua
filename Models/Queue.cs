using System;
using System.Collections.Generic;

namespace Cua.Models
{
    public class Queue
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int Limit { get; set; }
        public DateTime StartAt { get; set; }
        public bool Active { get; set; }
        public int CreatorId { get; set; }
        public User Creator { get; set; }
        public int RoomId { get; set; }
        public Room Room { get; set; }
        public ICollection<QueueUser> QueueUsers { get; set; }
        public Queue()
        {
            QueueUsers = new List<QueueUser>();
        }
    }
}