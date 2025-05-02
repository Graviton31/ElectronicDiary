using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Group
{
    public int IdGroup { get; set; }

    public string Name { get; set; } = null!;

    public sbyte? StudentCount { get; set; }

    public string Classroom { get; set; } = null!;

    public int IdEmployee { get; set; }

    public int IdSubject { get; set; }

    public int IdLocation { get; set; }

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual Employee IdEmployeeNavigation { get; set; } = null!;

    public virtual Location IdLocationNavigation { get; set; } = null!;

    public virtual Subject IdSubjectNavigation { get; set; } = null!;

    public virtual ICollection<Journal> Journals { get; set; } = new List<Journal>();

    public virtual ICollection<ScheduleEvent> ScheduleEvents { get; set; } = new List<ScheduleEvent>();

    public virtual ICollection<Student> IdStudents { get; set; } = new List<Student>();
}
