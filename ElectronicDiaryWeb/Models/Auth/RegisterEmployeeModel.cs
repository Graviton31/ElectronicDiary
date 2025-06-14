using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterEmployeeModel : BaseUserModel
    {
        [Required(ErrorMessage = "Роль обязательна")]
        public string Role { get; set; } = "сотрудник";

        [Required(ErrorMessage = "Должность обязательна")]
        public int PostId { get; set; }
    }
}
