using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class CreateRoomModel
    {
        [Required(ErrorMessage = "Room name not specified")]
        public string Name { get; set; }
    }
}