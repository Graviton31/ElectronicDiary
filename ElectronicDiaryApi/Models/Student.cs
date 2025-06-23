using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Student
{
    public int IdStudent { get; set; }

    public string? EducationName { get; set; }

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual User IdStudentNavigation { get; set; } = null!;

    public virtual ICollection<StudentsHasParent> StudentsHasParents { get; set; } = new List<StudentsHasParent>();

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();

    public virtual ICollection<Group> IdGroups { get; set; } = new List<Group>();
}
