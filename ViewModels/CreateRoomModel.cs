using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class CreateRoomModel
    {
        [Required(ErrorMessage = "Room name not specified")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Add organization name")]
        public string Company { get; set; }

        [Required(ErrorMessage = "Add brief info about room")]
        public string About { get; set; }
        public bool Private { get; set; }
        public bool Hidden { get; set; }
    }
}