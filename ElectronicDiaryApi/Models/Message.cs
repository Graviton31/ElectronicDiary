using System;
using System.Collections.Generic;

namespace ElectronicDiaryApi.Models;

public partial class Message
{
    public int IdMessage { get; set; }

    public string? Title { get; set; }

    public string? Text { get; set; }

    public int IdLesson { get; set; }

    public virtual Lesson IdLessonNavigation { get; set; } = null!;

    public virtual ICollection<Material> Materials { get; set; } = new List<Material>();
}
