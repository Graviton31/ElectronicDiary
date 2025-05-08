namespace ElectronicDiaryApi.ModelsDto.UsersView
{
    public class GroupDto
    {
        public int IdGroup { get; set; }
        public string Name { get; set; }
        public string Classroom { get; set; }
        public LocationDto Location { get; set; }
        public List<EmployeeDto> Teachers { get; set; } = new();
        public int MaxStudents { get; set; }  // StudentCount из модели
        public int CurrentStudents { get; set; } // Количество IdStudents
        public int IdSubject { get; set; }
        public string SubjectName { get; set; }
    }
}
