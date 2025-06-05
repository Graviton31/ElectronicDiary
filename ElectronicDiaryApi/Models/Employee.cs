using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Employee
{
    public int IdEmployee { get; set; }

    public int IdPost { get; set; }

    public virtual User IdEmployeeNavigation { get; set; } = null!;

    public virtual Post IdPostNavigation { get; set; } = null!;

    public virtual ICollection<Group> IdGroups { get; set; } = new List<Group>();

    public virtual ICollection<Subject> IdSubjects { get; set; } = new List<Subject>();
}
