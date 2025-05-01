using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Material
{
    public int IdMaterial { get; set; }

    public string? Name { get; set; }

    public int IdMessage { get; set; }

    public virtual Message IdMessageNavigation { get; set; } = null!;
}
