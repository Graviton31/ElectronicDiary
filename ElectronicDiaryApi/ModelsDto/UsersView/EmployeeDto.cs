namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class EmployeeDto
    {
        public int IdEmployee { get; set; }

        public string FullName { get; set; } = string.Empty;
        
        public DateOnly? BirthDate { get; set; }

        public string Login { get; set; }

        public string? Role { get; set; }

        public string? Phone { get; set; }

        public string Post { get; set; } = string.Empty;

        public List<SubjectWithGroupsDto> Subjects { get; set; } = new();
    }
}
