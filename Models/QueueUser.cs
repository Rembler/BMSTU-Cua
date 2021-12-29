using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Cua.Models
{
    public class QueueUser
    {
        public int QueueId { get; set; }
        public int UserId { get; set; }
        public Queue Queue { get; set; }
        public User User { get; set; }
        public int Place { get; set; }
    }
}