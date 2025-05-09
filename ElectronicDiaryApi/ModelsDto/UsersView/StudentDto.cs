using ElectronicDiaryApi.Models;

namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class StudentDto
    {
        public int IdStudent { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Login { get; set; }
        public string? EducationName { get; set; }
        public List<SubjectWithGroupsDto> Subjects { get; set; } = new();
        public List<ParentDto> Parents { get; set; } = new();
        public List<EnrollmentRequestDto> EnrollmentRequests { get; set; } = new();
    }
}
