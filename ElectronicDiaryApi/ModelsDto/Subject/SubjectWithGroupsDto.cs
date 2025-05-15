using ElectronicDiaryApi.ModelsDto.Group;

namespace ElectronicDiaryApi.ModelsDto.Subject
{
    public class SubjectWithGroupsDto
    {
        public int IdSubject { get; set; }
        public string Name { get; set; }
        public List<GroupDto> Groups { get; set; } = new();
    }
}
