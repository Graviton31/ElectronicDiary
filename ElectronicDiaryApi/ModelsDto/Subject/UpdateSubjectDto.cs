namespace ElectronicDiaryApi.ModelsDto.Subject
{
    public class UpdateSubjectDto
    {
        public string Name { get; set; }
        public string FullName { get; set; }
        public string? Description { get; set; }
        public sbyte Duration { get; set; }
        public sbyte LessonLength { get; set; }
        public string? Syllabus { get; set; }
        public bool? IsDelete { get; set; }
        public List<int> TeacherIds { get; set; } = new();

        // Конструктор для удобства инициализации
        public UpdateSubjectDto() { }
    }
}
