using ElectronicDiaryApi.ModelsDto.Group;
using ElectronicDiaryApi.ModelsDto.Subject;
using ElectronicDiaryApi.ModelsDto.UsersView;

namespace ElectronicDiaryWeb.ViewModel
{
    public class SubjectDetailsViewModel
    {
        public SubjectDto Subject { get; set; }
        public List<GroupDto> Groups { get; set; } = new();
        public List<EmployeeDto> Teachers => Subject.Teachers;
    }
}
