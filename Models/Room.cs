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
        public virtual User Admin { get; set; }
        public ICollection<User> Users { get; set; }
        public Room()
        {
            Users = new List<User>();
        }
    }
}