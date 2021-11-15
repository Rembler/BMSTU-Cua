using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class CreateRoomModel
    {
        [Required(ErrorMessage = "Не указано имя комнаты")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Не указана организация")]
        public string Company { get; set; }

        [Required(ErrorMessage = "Добавьте краткое описание комнаты")]
        public string About { get; set; }
        public bool Private { get; set; }
        public bool Hidden { get; set; }
    }
}