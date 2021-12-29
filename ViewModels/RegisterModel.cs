using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class RegisterModel
    {
        [Required(ErrorMessage ="Не указан Email")]
        [EmailAddress]
        public string Email { get; set; }

        [Required(ErrorMessage = "Не указано имя")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Не указана фамилия")]
        public string Surname { get; set; }
        
        public string Company { get; set; }
         
        [Required(ErrorMessage = "Не указан пароль")]
        [StringLength(20, ErrorMessage = "Пароль должен содержать как минимум {2} символов", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
         
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}