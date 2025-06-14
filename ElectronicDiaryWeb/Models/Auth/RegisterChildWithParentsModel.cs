using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterChildWithParentsModel : BaseUserModel
    {
        public string? EducationName { get; set; }
        public List<int> ParentIds { get; set; } = new();
        public string ParentRole { get; set; } = "родитель";
    }
}
