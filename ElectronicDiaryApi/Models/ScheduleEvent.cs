using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class ScheduleEvent
{
    public int IdEvent { get; set; }

    public string EventType { get; set; } = null!;

    public sbyte? WeelDay { get; set; }

    public DateOnly? PlannedDate { get; set; }

    public DateOnly? ActualDate { get; set; }

    public TimeOnly StartTime { get; set; }

    public TimeOnly EndTime { get; set; }

    public int IdGroup { get; set; }

    public int? IdOriginalEvent { get; set; }

    public virtual Group IdGroupNavigation { get; set; } = null!;

    public virtual ScheduleEvent? IdOriginalEventNavigation { get; set; }

    public virtual ICollection<ScheduleEvent> InverseIdOriginalEventNavigation { get; set; } = new List<ScheduleEvent>();
}
