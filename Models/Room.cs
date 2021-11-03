using System.Collections.Generic;

namespace Cua.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Company { get; set; }
        public string About { get; set; }
        public bool Private { get; set; }
        public bool Hidden { get; set; }
        public int AdminId { get; set; }
        public User Admin { get; set; }
        public ICollection<Queue> Queues { get; set; }
        public ICollection<RoomUser> RoomUsers { get; set; }
        public ICollection<Request> Requests { get; set; }
        public Room()
        {
            RoomUsers = new List<RoomUser>();
            Queues = new List<Queue>();
            Requests = new List<Request>();
        }
    }
}