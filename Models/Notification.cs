using System;

namespace Cua.Models
{
    public class Notification
    {
        public int Id { get; set; }
        public int UserId { get; set; }

        [System.Text.Json.Serialization.JsonIgnore]
        public User User { get; set; }
        public string Body { get; set; }
        public bool Closed { get; set; }
        public DateTime Date { get; set; }
    }
}