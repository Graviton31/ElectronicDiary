using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class StudentsHasParent
{
    public int IdStudent { get; set; }

    public int IdParent { get; set; }

    public string ParentRole { get; set; } = null!;

    public virtual Parent IdParentNavigation { get; set; } = null!;

    public virtual Student IdStudentNavigation { get; set; } = null!;
}
