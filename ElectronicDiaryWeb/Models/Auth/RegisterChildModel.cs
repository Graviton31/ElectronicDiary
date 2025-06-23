using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterChildModel : BaseUserModel
    {
        [Required(ErrorMessage = "Учебное заведение обязательно")]
        public string EducationName { get; set; }

        [Required(ErrorMessage = "Роль родителя обязательна")]
        public string ParentRole { get; set; } = "опекун"; // Добавляем поле
    }
}
