using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.ViewModel
{
    public class EditGroupViewModel
    {
        public int IdGroup { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Название группы")]
        public string Name { get; set; }

        [Display(Name = "Кабинет")]
        public string Classroom { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Локация")]
        public int SelectedLocationId { get; set; }

        [Required(ErrorMessage = "Выберите преподавателей")]
        [Display(Name = "Преподаватели")]
        public List<int> SelectedTeacherIds { get; set; } = new();

        [Display(Name = "Макс. студентов")]
        [Range(1, 30, ErrorMessage = "Допустимо от 1 до 30")]
        public sbyte MaxStudentCount { get; set; }

        [Display(Name = "Возраст с")]
        [Range(1, 18, ErrorMessage = "Допустимо от 1 до 18")]
        public string MinAge { get; set; }

        [Display(Name = "Возраст до")]
        [Range(1, 19, ErrorMessage = "Допустимо от 1 до 19")]
        public string MaxAge { get; set; }

        [HiddenInput]
        public bool IsDelete { get; set; }

        [HiddenInput]
        public bool OriginalIsDeleted { get; set; } // Для хранения исходного значения
    }
}
