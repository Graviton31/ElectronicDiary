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
        [AgeValidation(6, 120, ErrorMessage = "Некоректная дата рождения")]
        public DateTime BirthDate { get; set; }

        [Required(ErrorMessage = "Телефон обязателен")]
        [RegularExpression(@"^\+7\d{10}$", ErrorMessage = "Номер телефона указан не верно")]
        public string Phone { get; set; }
    }

    // Кастомный атрибут для проверки возраста
    public class AgeValidationAttribute : ValidationAttribute
    {
        private readonly int _minAge;
        private readonly int _maxAge;

        public AgeValidationAttribute(int minAge, int maxAge)
        {
            _minAge = minAge;
            _maxAge = maxAge;
        }

        protected override ValidationResult IsValid(object value, ValidationContext validationContext)
        {
            if (value is DateTime birthDate)
            {
                var age = DateTime.Today.Year - birthDate.Year;
                if (birthDate > DateTime.Today.AddYears(-age)) age--;

                if (age < _minAge || age > _maxAge)
                {
                    return new ValidationResult(ErrorMessage);
                }
            }
            return ValidationResult.Success;
        }
    }
}
