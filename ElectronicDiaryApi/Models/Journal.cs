using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Journal
{
    public int IdJournal { get; set; }

    public DateOnly StartDate { get; set; }

    public DateOnly EndDate { get; set; }

    public sbyte? LessonsCount { get; set; }

    public int IdGroup { get; set; }

    public virtual Group IdGroupNavigation { get; set; } = null!;

    public virtual ICollection<Lesson> Lessons { get; set; } = new List<Lesson>();
}
