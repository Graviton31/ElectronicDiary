using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class EnrollmentRequest
{
    public int IdRequests { get; set; }

    public string? Status { get; set; }

    public DateTime? CreatedAt { get; set; }

    public int IdParent { get; set; }

    public int IdStudent { get; set; }

    public int IdGroup { get; set; }

    public virtual Group IdGroupNavigation { get; set; } = null!;

    public virtual Parent IdParentNavigation { get; set; } = null!;

    public virtual Student IdStudentNavigation { get; set; } = null!;
}
