using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterChildWithParentsModel : BaseUserModel
    {
        [Required(ErrorMessage = "Учебное заведение обязательно")]
        public string EducationName { get; set; }

        public List<int> ParentIds { get; set; } = new();

        [Required(ErrorMessage = "Роль родителя обязательна")]
        public string ParentRole { get; set; } = "опекун";
    }
}
