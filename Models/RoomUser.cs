namespace Cua.Models
{
    public class RoomUser
    {
        public int RoomId { get; set; }
        public int UserId { get; set; }
        public Room Room { get; set; }
        public User User { get; set; }
        public bool IsModerator { get; set; }
    }
}