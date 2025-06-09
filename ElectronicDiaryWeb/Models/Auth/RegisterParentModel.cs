using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class RegisterParentModel
    {
        [Required]
        public string Login { get; set; }

        [Required]
        [DataType(DataType.Password)]
        public string Password { get; set; }

        [Required]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        [DataType(DataType.Password)]
        public string ConfirmPassword { get; set; }

        [Required]
        public string Name { get; set; }

        [Required]
        public string Surname { get; set; }

        public string? Patronymic { get; set; }

        [Required]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required]
        [Phone]
        public string Phone { get; set; }

        public string? Workplace { get; set; }
    }
}
