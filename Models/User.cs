using System.Collections.Generic;

namespace Cua.Models
{
    public class User
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string Surname { get; set; }
        public string Email { get; set; }
        public string Company { get; set; }
        public string Password { get; set; }
        public byte[] StoredSalt { get; set; }
        public string ConfirmationToken { get; set; }
        public bool IsConfirmed { get; set; }
        public ICollection<Room> AdminRooms { get; set; }
        public ICollection<Room> Rooms { get; set; }
        public User()
        {
            Rooms = new List<Room>();
        }
    }
}