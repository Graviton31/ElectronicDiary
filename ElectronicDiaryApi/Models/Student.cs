using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Student
{
    public int IdStudent { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronymic { get; set; }

    public DateOnly BirthDate { get; set; }

    public string? Phone { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? EducationName { get; set; }

    public bool? IsDelete { get; set; }

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public virtual ICollection<Group> IdGroups { get; set; } = new List<Group>();

    public virtual ICollection<Parent> IdParents { get; set; } = new List<Parent>();
}
