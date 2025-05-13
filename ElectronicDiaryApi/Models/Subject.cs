using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Subject
{
    public int IdSubject { get; set; }

    public string Name { get; set; } = null!;

    public string FullName { get; set; } = null!;

    public string? Description { get; set; }

    public sbyte Duration { get; set; }

    public sbyte LessonLength { get; set; }

    public bool? IsDelete { get; set; }

    public string? Syllabus { get; set; }

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual ICollection<Employee> IdEmployees { get; set; } = new List<Employee>();
}
