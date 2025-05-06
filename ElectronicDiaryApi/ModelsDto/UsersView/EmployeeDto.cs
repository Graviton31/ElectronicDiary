namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class EmployeeDto
    {
        public int IdEmployee { get; set; }

        public string? FullName { get; set; }

        public DateOnly? BirthDate { get; set; }

        public string Login { get; set; }

        public string? Role { get; set; }

        public string? Phone { get; set; }

        public string Post { get; set; }

        public List<SubjectWithGroupsDto> Subjects { get; set; } = new();
    }
}
