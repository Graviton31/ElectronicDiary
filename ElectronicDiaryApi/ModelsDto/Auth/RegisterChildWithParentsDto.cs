namespace ElectronicDiaryApi.ModelsDto.Auth
{
    public class RegisterChildWithParentsDto : BaseUserDto
    {
        public string? EducationName { get; set; }
        public List<int> ParentIds { get; set; } = new();
        public string ParentRole { get; set; } = "родитель";
    }
}
