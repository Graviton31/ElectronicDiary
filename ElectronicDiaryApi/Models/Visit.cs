using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Visit
{
    public int IdVisit { get; set; }

    public string? UnvisitedStatuses { get; set; }

    public string? Comment { get; set; }

    public int IdLesson { get; set; }

    public int IdStudent { get; set; }

    public virtual Lesson IdLessonNavigation { get; set; } = null!;

    public virtual Student IdStudentNavigation { get; set; } = null!;
}
