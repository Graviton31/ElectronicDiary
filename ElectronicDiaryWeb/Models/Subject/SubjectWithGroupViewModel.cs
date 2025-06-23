using ElectronicDiaryApi.ModelsDto;
using ElectronicDiaryWeb.Models.Group;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Subject
{
    public class SubjectWithGroupViewModel
    {
        // Данные предмета
        [Required(ErrorMessage = "Поле обязательно для заполнения")]
        [Display(Name = "Короткое название")]
        public string SubjectName { get; set; }

        [Required(ErrorMessage = "Поле обязательно для заполнения")]
        [Display(Name = "Полное название")]
        public string SubjectFullName { get; set; }

        [Display(Name = "Описание")]
        public string? SubjectDescription { get; set; }

        [Required(ErrorMessage = "Поле обязательно для заполнения")]
        [Range(1, 10, ErrorMessage = "Длительность должна быть от 1 до 10 месяцев")]
        [Display(Name = "Длительность курса (мес.)")]
        public sbyte Duration { get; set; }

        [Required(ErrorMessage = "Поле обязательно для заполнения")]
        [Range(1, 5, ErrorMessage = "Продолжительность урока должна быть от 1 до 5 академических часов")]
        [Display(Name = "Продолжительность урока (академ. часов)")]
        public sbyte LessonLength { get; set; }

        [Display(Name = "Программа предмета")]
        public string? Syllabus { get; set; }

        [Required(ErrorMessage = "Необходимо выбрать хотя бы одного преподавателя")]
        [Display(Name = "Преподаватели предмета")]
        public List<int> SelectedSubjectTeacherIds { get; set; } = new();

        // Список групп
        [Required(ErrorMessage = "Необходимо добавить хотя бы одну группу")]
        [MinLength(1, ErrorMessage = "Необходимо добавить хотя бы одну группу")]
        public List<GroupViewModel> Groups { get; set; } = new() { new GroupViewModel() };

        // Списки для выбора
        public List<SelectListItem> Locations { get; set; } = new();
    }
}