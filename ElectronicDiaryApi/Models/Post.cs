using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Post
{
    public int IdPost { get; set; }

    public string PostName { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
