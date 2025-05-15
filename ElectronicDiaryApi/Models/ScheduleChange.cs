using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class ScheduleChange
{
    public int IdScheduleChange { get; set; }

    public int IdGroup { get; set; }

    public string ChangeType { get; set; } = null!;

    public DateOnly? OldDate { get; set; }

    public DateOnly? NewDate { get; set; }

    public TimeOnly? NewStartTime { get; set; }

    public TimeOnly? NewEndTime { get; set; }

    public string? NewClassroom { get; set; }

    public int? IdSchedule { get; set; }

    public virtual Group IdGroupNavigation { get; set; } = null!;

    public virtual StandardSchedule? IdScheduleNavigation { get; set; }
}
