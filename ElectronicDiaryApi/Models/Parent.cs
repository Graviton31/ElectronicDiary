using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Parent
{
    public int IdParent { get; set; }

    public string Surname { get; set; } = null!;

    public string Name { get; set; } = null!;

    public string? Patronumic { get; set; }

    public string? Phone { get; set; }

    public string Login { get; set; } = null!;

    public byte[] Password { get; set; } = null!;

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual ICollection<Student> IdStudents { get; set; } = new List<Student>();
}
