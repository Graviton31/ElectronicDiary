using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Post
{
    public int IdPost { get; set; }

    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    public virtual ICollection<User> Users { get; set; } = new List<User>();
}
