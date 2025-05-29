using ElectronicDiaryWeb.Models.Group;
using Microsoft.AspNetCore.Mvc.Rendering;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Subject
{
    public class EditSubjectViewModel
    {

        public EditSubjectViewModel()
        {
            SelectedSubjectTeacherIds = new List<int>();
            ExistingGroups = new List<EditGroupViewModel>();
            Locations = new List<SelectListItem>();
            TeacherNames = new Dictionary<int, string>();
        }

        public int IdSubject { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Короткое название")]
        public string SubjectName { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Полное название")]
        public string SubjectFullName { get; set; }

        [Display(Name = "Описание")]
        public string SubjectDescription { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Range(1, 10, ErrorMessage = "Длительность от 1 до 10")]
        [Display(Name = "Длительность курса (лет)")]
        public sbyte Duration { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Range(1, 5, ErrorMessage = "Длительность урока от 1 до 5")]
        [Display(Name = "Продолжительность урока (часов)")]
        public sbyte LessonLength { get; set; }

        [Display(Name = "Программа предмета")]
        public string? Syllabus { get; set; }

        [Display(Name = "Преподаватели предмета")]
        public List<int> SelectedSubjectTeacherIds { get; set; } = new();

        public List<EditGroupViewModel> ExistingGroups { get; set; } = new();
        public List<SelectListItem> Locations { get; set; } = new();
        public Dictionary<int, string> TeacherNames { get; set; } = new();
    }
}