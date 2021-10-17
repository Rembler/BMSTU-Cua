using System;
using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class CreateQueueModel
    {
        [Required(ErrorMessage = "Queue name not specified")]
        public string Name { get; set; }
        public int? Limit { get; set; }
        public DateTime StartAt { get; set; }
        public int RoomId { get; set; }
    }
}