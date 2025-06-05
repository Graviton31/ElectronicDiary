using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class User
{
    public int IdUser { get; set; }

    public string Name { get; set; } = null!;

    public string Surname { get; set; } = null!;

    public string? Patronymic { get; set; }

    public DateOnly BirthDate { get; set; }

    public string Phone { get; set; } = null!;

    public string Login { get; set; } = null!;

    public string Password { get; set; } = null!;

    public string? RefreshToken { get; set; }

    public DateTime? RefreshTokenExpiryTime { get; set; }

    public string Role { get; set; } = null!;

    public bool? IsDelete { get; set; }

    public virtual Employee? Employee { get; set; }

    public virtual Parent? Parent { get; set; }

    public virtual Student? Student { get; set; }
}
