using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Student
{
    public int IdStudent { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronumic { get; set; }

    public string? Phone { get; set; }

    public string Login { get; set; } = null!;

    public byte[] Password { get; set; } = null!;

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public virtual ICollection<Group> IdGroups { get; set; } = new List<Group>();

    public virtual ICollection<Parent> IdParents { get; set; } = new List<Parent>();
}
