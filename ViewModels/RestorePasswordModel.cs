using System.ComponentModel.DataAnnotations;
 
namespace Cua.ViewModels
{
    public class RestorePasswordModel
    {
        [Required(ErrorMessage = "Не указан Email")]
        [EmailAddress(ErrorMessage = "Укажите существующий Email")]
        public string Email { get; set; }
    }
}