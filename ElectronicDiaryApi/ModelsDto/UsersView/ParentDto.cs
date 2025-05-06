using ElectronicDiaryApi.Models;

namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class ParentDto
    {
        public int IdParent { get; set; }
        public string FullName { get; set; }
        public DateOnly BirthDate { get; set; }
        public string Phone { get; set; }
        public string Login { get; set; }
        public string ParentRole { get; set; }
        public List<StudentDto> Students { get; set; } = new();
        public List<EnrollmentRequestDto> EnrollmentRequests { get; set; } = new();
    }
}
