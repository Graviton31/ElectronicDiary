using ElectronicDiaryApi.ModelsDto.UsersView;

namespace ElectronicDiaryApi.ModelsDto.Group
{
    public class GroupDto
    {
        public int IdGroup { get; set; }
        public string Name { get; set; }
        public LocationDto Location { get; set; }
        public List<EmployeeDto> Teachers { get; set; } = new();
        public sbyte MaxStudentCount { get; set; }  // StudentCount из модели
        public int CurrentStudents { get; set; } // Количество IdStudents
        public int IdSubject { get; set; }
        public string SubjectName { get; set; }
        public string MinAge { get; set; } = null!;
        public string MaxAge { get; set; } = null!;
        //public bool? IsDelete { get; set; }
    }
}
