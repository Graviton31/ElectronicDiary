﻿using System.ComponentModel.DataAnnotations;

namespace ElectronicDiaryWeb.Models.Group
{
    public class GroupViewModel
    {
        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Название группы")]
        public string Name { get; set; }

        [Required(ErrorMessage = "Обязательное поле")]
        [Display(Name = "Локация")]
        public int SelectedLocationId { get; set; }

        [Required(ErrorMessage = "Выберите хотя бы одного преподавателя")]
        [Display(Name = "Преподаватели группы")]
        public List<int> SelectedTeacherIds { get; set; } = new();

        [Display(Name = "Макс. учеников")]
        [Range(1, 30, ErrorMessage = "Допустимо от 1 до 30")]
        public sbyte? MaxStudentCount { get; set; }

        [Display(Name = "Ворзраст с")]
        [Range(1, 18, ErrorMessage = "Допустимо от 1 до 18")]
        public string MinAge { get; set; } = null!;

        [Display(Name = "Ворзраст до")]
        [Range(1, 19, ErrorMessage = "Допустимо от 1 до 19")]
        public string MaxAge { get; set; } = null!;
    }
}
