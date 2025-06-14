using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterChildModel : BaseUserModel
    {
        public string? EducationName { get; set; }
    }
}
