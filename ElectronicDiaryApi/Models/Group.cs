using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Group
{
    public int IdGroup { get; set; }

    public string Name { get; set; } = null!;

    public sbyte MaxStudentCount { get; set; }

    public string MinAge { get; set; } = null!;

    public string MaxAge { get; set; } = null!;

    public bool? IsDelete { get; set; }

    public int IdSubject { get; set; }

    public int IdLocation { get; set; }

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual Location IdLocationNavigation { get; set; } = null!;

    public virtual Subject IdSubjectNavigation { get; set; } = null!;

    public virtual ICollection<Journal> Journals { get; set; } = new List<Journal>();

    public virtual ICollection<ScheduleChange> ScheduleChanges { get; set; } = new List<ScheduleChange>();

    public virtual ICollection<StandardSchedule> StandardSchedules { get; set; } = new List<StandardSchedule>();

    public virtual ICollection<Employee> IdEmployees { get; set; } = new List<Employee>();

    public virtual ICollection<Student> IdStudents { get; set; } = new List<Student>();
}
