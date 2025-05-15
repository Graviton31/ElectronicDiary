using ElectronicDiaryApi.ModelsDto.UsersView;

namespace ElectronicDiaryApi.ModelsDto.Subject
{
    public class SubjectDto
    {
        public int IdSubject { get; set; }
        public string Name { get; set; }
        public string FullName { get; set; }
        public string Description { get; set; }
        public sbyte Duration { get; set; }
        public sbyte LessonLength { get; set; }
        public string? Syllabus { get; set; }
        public bool? IsDelete { get; set; }
        public List<EmployeeDto> Teachers { get; set; } = new();
    }
}
