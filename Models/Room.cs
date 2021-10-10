using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cua.Models
{
    public class Room
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public int AdminId { get; set; }
        public ICollection<User> Users { get; set; }
        public Room()
        {
            Users = new List<User>();
        }
    }
}