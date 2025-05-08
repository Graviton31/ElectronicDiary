using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.ViewModel
{
    public class GroupViewModel
    {
        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Название группы")]
        public string Name { get; set; }

        [Display(Name = "Аудитория")]
        public string Classroom { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Локация")]
        public int SelectedLocationId { get; set; }

        [Required(ErrorMessage = "Выберите хотя бы одного преподавателя")]
        [Display(Name = "Преподаватели группы")]
        public List<int> SelectedTeacherIds { get; set; } = new();
    }
}
