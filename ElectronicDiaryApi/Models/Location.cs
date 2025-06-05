using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Location
{
    public int IdLocation { get; set; }

    public string? Name { get; set; }

    public string Address { get; set; } = null!;

    public virtual ICollection<Group> Groups { get; set; } = new List<Group>();
}
