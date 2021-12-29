using System.ComponentModel.DataAnnotations;

namespace Cua.ViewModels
{
    public class ChangePasswordModel
    {
        public int UserId { get; set; }  
           
        [Required(ErrorMessage = "Не указан пароль")]
        [StringLength(20, ErrorMessage = "Пароль должен содержать как минимум {2} символов", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; }
         
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }
    }
}