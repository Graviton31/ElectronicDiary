using ElectronicDiaryApi.Models;

namespace ElectronicDiaryApi.ModelsDto
{
    public class ParentDto
    {
        public int IdParent { get; set; }
        public string FullName { get; set; }
        public string Phone { get; set; }
        public string Login { get; set; }
        public List<StudentDto> Students { get; set; } = new();
        public List<EnrollmentRequestDto> EnrollmentRequests { get; set; } = new();
    }
}
