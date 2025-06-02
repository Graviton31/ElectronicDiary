using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Lesson
{
    public int IdLesson { get; set; }

    public DateOnly? LessonDate { get; set; }

    public int IdJournal { get; set; }

    public virtual Journal IdJournalNavigation { get; set; } = null!;

    public virtual ICollection<Message> Messages { get; set; } = new List<Message>();

    public virtual ICollection<Visit> Visits { get; set; } = new List<Visit>();
}
