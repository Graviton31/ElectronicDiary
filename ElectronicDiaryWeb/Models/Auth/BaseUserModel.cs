using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Auth
{
    public class BaseUserModel
    {
        [Required(ErrorMessage = "Логин обязателен")]
        public string Login { get; set; }

        [Required(ErrorMessage = "Пароль обязателен")]
        [DataType(DataType.Password)]
        [MinLength(6, ErrorMessage = "Пароль должен содержать минимум 6 символов")]
        public string Password { get; set; }

        [Required(ErrorMessage = "Подтверждение пароля обязательно")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Пароли не совпадают")]
        public string ConfirmPassword { get; set; }

        [Required(ErrorMessage = "Имя обязательно")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Фамилия обязательна")]
        public string Surname { get; set; }

        public string? Patronymic { get; set; }

        [Required(ErrorMessage = "Дата рождения обязательна")]
        [DataType(DataType.Date)]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Телефон обязателен")]
        [Phone(ErrorMessage = "Неверный формат телефона")]
        public string Phone { get; set; }
    }
}
