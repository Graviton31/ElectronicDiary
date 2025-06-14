using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterParentModel : BaseUserModel
    {
        public string? Workplace { get; set; }
    }
}
