using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Parent
{
    public int IdParent { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronymic { get; set; }

    public DateOnly BirthDate { get; set; }

    public string? Phone { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string ParentRole { get; set; } = null!;

    public bool? IsDelete { get; set; }

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual ICollection<Student> IdStudents { get; set; } = new List<Student>();
}
