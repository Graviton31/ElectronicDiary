namespace ElectronicDiaryApi.ModelsDto
{
    public class CreateSubjectDto
    {
        public string Name { get; set; }        // Короткое название (например, "Математика")
        public string FullName { get; set; }    // Полное название (например, "Углубленный курс математики")
        public string? Description { get; set; } // Описание (опционально)
        public sbyte Duration { get; set; }     // Длительность курса (в месяцах)
        public sbyte LessonLength { get; set; } // Продолжительность урока (в минутах)
    }
}
