namespace ElectronicDiaryApi.ModelsDto
{
    public class UpdateGroupDto
    {
        public string Name { get; set; }
        public sbyte MaxStudentCount { get; set; }
        public string Classroom { get; set; }
        public string MinAge { get; set; }
        public string MaxAge { get; set; }
        public bool? IsDelete { get; set; }
        public int IdLocation { get; set; }
        public List<int> TeacherIds { get; set; } = new();

        public UpdateGroupDto() { }
    }
}
