using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Employee
{
    public int IdEmployee { get; set; }

    public string? Surname { get; set; }

    public string? Name { get; set; }

    public string? Patronymic { get; set; }

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? Phone { get; set; }

    public DateOnly? BirthDate { get; set; }

    public string Role { get; set; } = null!;

    public bool? IsDelete { get; set; }

    public int IdPost { get; set; }

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();

    public virtual Post IdPostNavigation { get; set; } = null!;

    public virtual ICollection<Subject> IdSubjects { get; set; } = new List<Subject>();
}
