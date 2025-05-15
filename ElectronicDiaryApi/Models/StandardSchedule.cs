using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class StandardSchedule
{
    public int IdStandardSchedule { get; set; }

    public int IdGroup { get; set; }

    public sbyte WeekDay { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public string Classroom { get; set; } = null!;

    public virtual Group IdGroupNavigation { get; set; } = null!;

    public virtual ICollection<ScheduleChange> ScheduleChanges { get; set; } = new List<ScheduleChange>();
}
