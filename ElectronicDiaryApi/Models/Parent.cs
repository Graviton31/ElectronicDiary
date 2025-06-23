using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Parent
{
    public int IdParent { get; set; }

    public string? Workplace { get; set; }

    public virtual ICollection<EnrollmentRequest> EnrollmentRequests { get; set; } = new List<EnrollmentRequest>();

    public virtual User IdParentNavigation { get; set; } = null!;

    public virtual ICollection<StudentsHasParent> StudentsHasParents { get; set; } = new List<StudentsHasParent>();
}
